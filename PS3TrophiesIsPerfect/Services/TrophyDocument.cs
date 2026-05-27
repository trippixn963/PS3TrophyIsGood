using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PS3TrophiesIsPerfect.Models;
using TROPHYParser;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Wraps the frozen TROPHYParser core (TROPCONF/TROPTRNS/TROPUSR) for one open trophy folder:
    /// load, edit, save, plus the PSNProfiles name-match and the night-session relocation. All the
    /// behavioural logic is ported from the WinForms MainAPP so the proven rules carry over unchanged.
    /// </summary>
    public sealed class TrophyDocument
    {
        public const string DefaultProfile = "Default Profile";

        private TROPCONF _tconf;
        private TROPTRNS _tpsn;
        private TROPUSR _tusr;
        private string _path;
        private string _pathTemp;
        private DateTime _lastSyncTrophyTime;
        private int _baseGameCount;

        public bool IsOpen { get; private set; }
        public string GameTitle => _tconf?.title_name ?? string.Empty;
        public string GamePath => _path;

        // ---- open / close / save -------------------------------------------------------------

        public void Open(string folder)
        {
            Close();
            _path = folder;
            _pathTemp = Utility.CopyTrophyDirToTemp(folder);
            Utility.DecryptTrophy(_pathTemp);
            _tconf = new TROPCONF(_pathTemp, false);
            _tpsn = new TROPTRNS(_pathTemp, false);
            _tusr = new TROPUSR(_pathTemp, false);

            _lastSyncTrophyTime = _tusr.LastSyncTime;
            if (DateTime.Compare(_tpsn.LastSyncTime, _tusr.LastSyncTime) > 0)
                _lastSyncTrophyTime = _tpsn.LastSyncTime;

            IsOpen = true;
        }

        public void Save(string profile = DefaultProfile)
        {
            if (!IsOpen)
                return;
            _tpsn.Save();
            _tusr.Save();
            string encPathTemp = Utility.GetTemporaryDirectory();
            try
            {
                Utility.CopyTrophyData(_pathTemp, encPathTemp, false);
                Utility.EncryptTrophy(encPathTemp, profile);
                Utility.CopyTrophyData(encPathTemp, _path, true);
            }
            finally
            {
                Utility.DeleteDirectory(encPathTemp);
            }
        }

        public void Close()
        {
            string temp = _pathTemp;
            _tconf = null;
            _tpsn = null;
            _tusr = null;
            _path = null;
            _pathTemp = null;
            IsOpen = false;
            if (!string.IsNullOrEmpty(temp))
            {
                try
                {
                    Utility.DeleteDirectory(new DirectoryInfo(temp).Parent.FullName);
                }
                catch
                { /* best effort */
                }
            }
        }

        // ---- state queries -------------------------------------------------------------------

        private bool IsTrophyGot(int id) =>
            _tpsn[id].HasValue || _tusr.trophyTimeInfoTable[id].IsGet;

        private bool IsTrophySync(int id) =>
            (_tpsn[id].HasValue && _tpsn[id].Value.IsSync) || _tusr.trophyTimeInfoTable[id].IsSync;

        private int GetCountBaseTrophiesGot()
        {
            int n = 0;
            for (int i = 0; i < _tconf.trophys.Count; i++)
                if (_tconf[i].gid == 0 && IsTrophyGot(i))
                    n++;
            return n;
        }

        private DateTime? UnlockTimeOf(int i)
        {
            if (_tpsn[i].HasValue && _tpsn[i].Value.Time.Ticks > 0)
                return _tpsn[i].Value.Time;
            DateTime t = _tusr.trophyTimeInfoTable[i].Time;
            return t.Ticks > 0 ? t : (DateTime?)null;
        }

        // ---- row projection ------------------------------------------------------------------

        public CompletionStats Stats()
        {
            int totalGrade = 0,
                getGrade = 0,
                got = 0;
            for (int i = 0; i < _tconf.Count; i++)
            {
                int g = GradeOf((TropType)_tusr.trophyTypeTable[i].Type);
                totalGrade += g;
                if (IsTrophyGot(i))
                {
                    getGrade += g;
                    got++;
                }
            }
            int pct = totalGrade > 0 ? (int)Math.Round(getGrade * 100.0 / totalGrade) : 0;
            return new CompletionStats(
                got,
                _tconf.Count,
                getGrade,
                totalGrade,
                pct,
                _tconf.title_name
            );
        }

        private static int GradeOf(TropType t)
        {
            switch (t)
            {
                case TropType.Platinum:
                    return (int)TropGrade.Platinum;
                case TropType.Gold:
                    return (int)TropGrade.Gold;
                case TropType.Silver:
                    return (int)TropGrade.Silver;
                case TropType.Bronze:
                    return (int)TropGrade.Bronze;
                default:
                    return 0;
            }
        }

        public List<TrophyRow> BuildRows()
        {
            var diffs = ComputeTimeDiffStrings();
            var rows = new List<TrophyRow>();
            for (int i = 0; i < _tconf.Count; i++)
            {
                if (_tconf[i].gid == 0)
                    _baseGameCount = i;

                bool got,
                    synced;
                DateTime? time;
                if (_tpsn[i].HasValue)
                {
                    got = true;
                    synced = _tpsn[i].Value.IsSync;
                    time = _tpsn[i].Value.Time;
                }
                else
                {
                    var tti = _tusr.trophyTimeInfoTable[i];
                    got = tti.IsGet;
                    synced = tti.IsSync;
                    time = tti.Time.Ticks > 0 ? tti.Time : (DateTime?)null;
                }

                rows.Add(
                    new TrophyRow
                    {
                        Id = i,
                        Name = _tconf[i].name,
                        Detail = _tconf[i].detail,
                        Type = TypeLetter(_tconf[i].ttype),
                        Got = got,
                        Synced = synced,
                        Time = got ? time : null,
                        Elapsed = diffs.TryGetValue(i, out string d) ? d : string.Empty,
                        Icon = LoadIcon(_tconf[i].id),
                    }
                );
            }
            return rows;
        }

        /// <summary>
        /// Builds the merged donor-vs-you comparison, sorted by the donor's unlock order. For each donor
        /// trophy it pairs the donor's time/gap with your current applied time/gap (matched by name) and a
        /// verdict on the gap: exact (bursts), slower (intended), faster (a red flag), or — (no data).
        /// </summary>
        public List<ComparisonRow> BuildComparison(IReadOnlyList<DonorEntry> donor)
        {
            var local = new Dictionary<string, int>(StringComparer.Ordinal); // normalised name -> local index
            if (_tconf != null)
                for (int i = 0; i < _tconf.Count; i++)
                {
                    string key = NormalizeTrophyName(_tconf[i].name);
                    if (key.Length > 0 && !local.ContainsKey(key))
                        local[key] = i;
                }

            var sorted = donor.Where(d => d != null && d.Date != 0).OrderBy(d => d.Date).ToList();
            var rows = new List<ComparisonRow>();
            DateTime donorPrev = default;
            DateTime? myPrev = null;

            for (int k = 0; k < sorted.Count; k++)
            {
                DateTime dTime = sorted[k].Date.TimeStampToDateTime();
                TimeSpan? dGap = k == 0 ? (TimeSpan?)null : dTime - donorPrev;

                string type = string.Empty;
                System.Windows.Media.ImageSource icon = null;
                DateTime? myTime = null;
                if (local.TryGetValue(NormalizeTrophyName(sorted[k].Name), out int li))
                {
                    type = TypeLetter(_tconf[li].ttype);
                    icon = LoadIcon(_tconf[li].id);
                    myTime = UnlockTimeOf(li);
                }
                TimeSpan? myGap =
                    (k > 0 && myTime.HasValue && myPrev.HasValue)
                        ? myTime.Value - myPrev.Value
                        : (TimeSpan?)null;

                var row = new ComparisonRow
                {
                    Order = k + 1,
                    Name = sorted[k].Name,
                    Type = type,
                    Icon = icon,
                    DonorTimeText = dTime.ToString("yyyy/MM/dd  HH:mm:ss"),
                    DonorGapText = dGap == null ? "" : "+" + FormatSpan(dGap.Value),
                    MyTimeText = myTime.HasValue
                        ? myTime.Value.ToString("yyyy/MM/dd  HH:mm:ss")
                        : "—",
                    MyGapText = myGap == null ? "" : "+" + FormatSpan(myGap.Value),
                };

                if (!myTime.HasValue)
                {
                    row.Match = "missing";
                    row.MatchText = "—";
                }
                else if (dGap == null)
                {
                    row.Match = "first";
                    row.MatchText = "—";
                }
                else if (myGap == null)
                {
                    row.Match = "missing";
                    row.MatchText = "—";
                }
                else
                {
                    long sec = (long)System.Math.Round((myGap.Value - dGap.Value).TotalSeconds);
                    if (sec == 0)
                    {
                        row.Match = "exact";
                        row.MatchText = "✓ exact";
                    }
                    else if (sec > 0)
                    {
                        row.Match = "slower";
                        row.MatchText = "+" + FormatSpan(System.TimeSpan.FromSeconds(sec));
                    }
                    else
                    {
                        row.Match = "faster";
                        row.MatchText = "⚠ −" + FormatSpan(System.TimeSpan.FromSeconds(-sec));
                    }
                }

                rows.Add(row);
                donorPrev = dTime;
                myPrev = myTime;
            }
            return rows;
        }

        /// <summary>Normalises the parser's ttype ("P"/"G"/"S"/"B" or longer) to a single letter.</summary>
        private static string TypeLetter(string ttype)
        {
            if (string.IsNullOrEmpty(ttype))
                return "";
            return ttype.Substring(0, 1).ToUpperInvariant();
        }

        private System.Windows.Media.ImageSource LoadIcon(int tropId)
        {
            try
            {
                string file = Path.Combine(_path, "TROP" + tropId.ToString("000") + ".PNG");
                return ImageLoad.FromFile(file);
            }
            catch
            {
                return null;
            }
        }

        public System.Windows.Media.ImageSource LoadGameIcon()
        {
            try
            {
                return ImageLoad.FromFile(Path.Combine(_path, "ICON0.PNG"));
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<int, string> ComputeTimeDiffStrings()
        {
            var unlocked = new List<KeyValuePair<int, DateTime>>();
            for (int i = 0; i < _tconf.Count; i++)
            {
                DateTime? t = UnlockTimeOf(i);
                if (t.HasValue)
                    unlocked.Add(new KeyValuePair<int, DateTime>(i, t.Value));
            }
            unlocked.Sort((a, b) => a.Value.CompareTo(b.Value));

            var result = new Dictionary<int, string>();
            for (int k = 0; k < unlocked.Count; k++)
            {
                if (k == 0)
                {
                    result[unlocked[k].Key] = string.Empty;
                    continue;
                }
                TimeSpan elapsed = unlocked[k].Value - unlocked[0].Value;
                if (k == 1)
                    result[unlocked[k].Key] = FormatSpan(elapsed);
                else
                {
                    TimeSpan gap = unlocked[k].Value - unlocked[k - 1].Value;
                    result[unlocked[k].Key] = FormatSpan(elapsed) + " (+" + FormatSpan(gap) + ")";
                }
            }
            return result;
        }

        private static string FormatSpan(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero;
            var sb = new System.Text.StringBuilder();
            if (ts.Days > 0)
                sb.Append(ts.Days).Append("d ");
            if (ts.Hours > 0)
                sb.Append(ts.Hours).Append("h ");
            if (ts.Minutes > 0)
                sb.Append(ts.Minutes).Append("m ");
            sb.Append(ts.Seconds).Append('s');
            return sb.ToString();
        }

        // ---- single-trophy edits -------------------------------------------------------------

        /// <summary>Throws with a user-facing message when the edit isn't allowed.</summary>
        public void Unlock(int id, DateTime time)
        {
            if (IsTrophySync(id))
                throw new InvalidOperationException(
                    "Trophy already synchronized. Can't be modified."
                );
            if (id == 0 && _tconf.HasPlatinium && GetCountBaseTrophiesGot() < _baseGameCount)
                throw new InvalidOperationException(
                    "You can't unlock the platinum while other trophies are still locked."
                );
            ValidateDate(time);
            _tpsn.PutTrophy(id, _tusr.trophyTypeTable[id].Type, time);
            _tusr.UnlockTrophy(id, time);
        }

        public void ChangeTime(int id, DateTime time)
        {
            if (IsTrophySync(id))
                throw new InvalidOperationException(
                    "Trophy already synchronized. Can't be modified."
                );
            ValidateDate(time);
            _tpsn.ChangeTime(id, time);
            TROPUSR.TrophyTimeInfo tti = _tusr.trophyTimeInfoTable[id];
            tti.Time = time;
            _tusr.trophyTimeInfoTable[id] = tti;
        }

        public void Delete(int id)
        {
            if (IsTrophySync(id))
                throw new InvalidOperationException(
                    "Trophy already synchronized. Can't be modified."
                );
            if (id != 0 && _tconf[id].gid == 0 && IsTrophyGot(0))
                throw new InvalidOperationException(
                    "You can't lock other trophies while the platinum is unlocked."
                );
            _tpsn.DeleteTrophyByID(id);
            _tusr.LockTrophy(id);
        }

        public bool HasTime(int id) => _tpsn[id].HasValue;

        public bool IsSynced(int id) => IsTrophySync(id);

        public bool IsGot(int id) => IsTrophyGot(id);

        public DateTime? TimeOf(int id) => UnlockTimeOf(id);

        public DateTime EarliestAllowed => _lastSyncTrophyTime;

        private void ValidateDate(DateTime t)
        {
            if (DateTime.Compare(_lastSyncTrophyTime, t) > 0)
                throw new InvalidOperationException(
                    "The last trophy synchronized with PSN is dated "
                        + _lastSyncTrophyTime
                        + ". Pick a later date."
                );
        }

        public void ClearAll()
        {
            TROPTRNS.TrophyInfo? ti = _tpsn.PopTrophy();
            while (ti.HasValue)
            {
                _tusr.LockTrophy(ti.Value.TrophyID);
                ti = _tpsn.PopTrophy();
            }
        }

        // ---- PSNProfiles import + apply ------------------------------------------------------

        /// <summary>
        /// Maps scraped trophies onto the loaded game's table by normalised name. Returns a per-index
        /// times array (Unix seconds, 0 = unmatched), plus the matched/unmatched name lists.
        /// </summary>
        public long[] MatchScrape(
            IEnumerable<ScrapedTrophy> scraped,
            out int matched,
            out List<string> unmatched
        )
        {
            var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _tconf.Count; i++)
            {
                string key = NormalizeTrophyName(_tconf[i].name);
                if (key.Length > 0 && !indexByName.ContainsKey(key))
                    indexByName[key] = i;
            }

            int count = _tusr.trophyTimeInfoTable.Count;
            var times = new long[count];
            matched = 0;
            unmatched = new List<string>();
            foreach (var p in scraped)
            {
                if (string.IsNullOrWhiteSpace(p.Name))
                    continue;
                if (
                    indexByName.TryGetValue(NormalizeTrophyName(p.Name), out int idx)
                    && idx < count
                )
                {
                    times[idx] = p.Date;
                    matched++;
                }
                else
                    unmatched.Add(p.Name);
            }
            return times;
        }

        /// <summary>Clears existing unlocks (when there's anything to apply) and writes the new times.</summary>
        public void ApplyTimes(long[] times)
        {
            if (times.Any(t => t != 0))
                ClearAll();
            for (int i = 0; i < _tusr.trophyTimeInfoTable.Count && i < times.Length; i++)
            {
                if (!_tpsn[i].HasValue && times[i] != 0)
                {
                    var time = times[i].TimeStampToDateTime();
                    _tusr.UnlockTrophy(i, time);
                    _tpsn.PutTrophy(i, _tusr.trophyTypeTable[i].Type, time);
                }
            }
        }

        public bool HasPlatinum => _tconf != null && _tconf.HasPlatinium;

        private static string NormalizeTrophyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            string n = name.Normalize(System.Text.NormalizationForm.FormKC);
            var sb = new System.Text.StringBuilder(n.Length);
            foreach (char c in n)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        // ---- night-session relocation (invariants preserved verbatim) ------------------------

        /// <summary>
        /// Rebuilds an imported unlock sequence as realistic nightly play sessions from <paramref name="startDate"/>
        /// to today, finishing with the platinum earned today. Bursts (≤60s) and the platinum pop-gap keep the
        /// donor's EXACT gaps; every other gap is the donor's plus a few minutes (never faster); nothing is ever
        /// dated in the future. Mutates <paramref name="times"/> in place. (Ported from MainAPP.MaybeRelocateToNightSessions.)
        /// </summary>
        public RelocationResult RelocateToNightSessions(long[] timesArr, DateTime startDate) =>
            RelocationEngine.Rebuild(timesArr, startDate, HasPlatinum);
    }

    public struct CompletionStats
    {
        public int Got,
            Total,
            GetGrade,
            TotalGrade,
            Percent;
        public string Title;

        public CompletionStats(
            int got,
            int total,
            int getGrade,
            int totalGrade,
            int percent,
            string title
        )
        {
            Got = got;
            Total = total;
            GetGrade = getGrade;
            TotalGrade = totalGrade;
            Percent = percent;
            Title = title;
        }
    }
}
