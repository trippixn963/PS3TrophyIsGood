using System;
using System.Collections.Generic;
using System.Linq;

namespace PS3TrophiesIsPerfect.Services
{
    public struct RelocationResult
    {
        public int Sessions;
        public bool PlatEarned;
        public DateTime First, Last;
        public RelocationResult(int sessions, bool platEarned, DateTime first, DateTime last)
        { Sessions = sessions; PlatEarned = platEarned; First = first; Last = last; }
    }

    /// <summary>
    /// Rebuilds an imported unlock sequence as realistic nightly play sessions from a start date through
    /// today, finishing with the platinum earned today. HARD INVARIANTS (do not change): bursts (≤60s) and
    /// the platinum pop-gap keep the donor's EXACT gaps; every other gap is the donor's plus a few minutes
    /// (always SLOWER, never faster); nothing is ever dated in the future. Mutates <paramref name="timesArr"/>.
    /// Moved verbatim from TrophyDocument; the only external input is <paramref name="hasPlatinum"/>.
    /// </summary>
    public static class RelocationEngine
    {
        public static RelocationResult Rebuild(long[] timesArr, DateTime startDate, bool hasPlatinum)
        {
            const int SessionStartHour = 22;
            const int NightStartJitterMinutes = 75;
            const int MinSessionMinutes = 150;
            const int MaxSessionMinutes = 300;
            const long BurstGapSeconds = 60;
            const int MinExtraMinutes = 1;
            const int MaxExtraMinutes = 10;
            const int LullChancePercent = 12;
            const int LullMinMinutes = 15;
            const int LullMaxMinutes = 50;

            var times = timesArr.ToList();

            if (startDate > DateTime.Today)
                startDate = DateTime.Today;

            var original = new List<long>(times);
            var seq = new List<KeyValuePair<int, long>>();
            for (int i = 0; i < original.Count; i++)
                if (original[i] != 0)
                    seq.Add(new KeyValuePair<int, long>(i, original[i]));
            seq.Sort((a, b) => a.Value.CompareTo(b.Value));
            if (seq.Count == 0)
                return new RelocationResult(0, false, DateTime.MinValue, DateTime.MinValue);

            var rand = new Random();
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ToUnix(DateTime dt) => (long)(dt - epoch).TotalSeconds;

            var sessionStart = new List<int> { 0 };
            var relOffset = new long[seq.Count];
            long elapsed = 0;
            long nightLenSec = (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
            for (int k = 1; k < seq.Count; k++)
            {
                long gap = seq[k].Value - seq[k - 1].Value;
                long add;
                if (gap <= BurstGapSeconds)
                {
                    add = gap;
                }
                else
                {
                    long extraMin = rand.Next(MinExtraMinutes, MaxExtraMinutes + 1);
                    if (rand.Next(100) < LullChancePercent)
                        extraMin += rand.Next(LullMinMinutes, LullMaxMinutes + 1);
                    add = gap + extraMin * 60 + rand.Next(0, 60);

                    if (elapsed + add > nightLenSec)
                    {
                        sessionStart.Add(k);
                        relOffset[k] = 0;
                        elapsed = 0;
                        nightLenSec = (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
                        continue;
                    }
                }
                relOffset[k] = relOffset[k - 1] + add;
                elapsed += add;
            }
            int sessions = sessionStart.Count;

            var nightDay = new DateTime[sessions];
            int lead = sessions - 1;
            if (lead >= 1)
            {
                nightDay[0] = startDate;
                int availDays = Math.Max(0, (int)(DateTime.Today.AddDays(-2) - startDate).TotalDays);
                if (lead - 1 <= availDays)
                {
                    var offsets = new List<int>();
                    for (int d = 1; d <= availDays; d++)
                        offsets.Add(d);
                    for (int i = offsets.Count - 1; i > 0; i--)
                    {
                        int j = rand.Next(i + 1);
                        int tmp = offsets[i]; offsets[i] = offsets[j]; offsets[j] = tmp;
                    }
                    var chosen = offsets.Take(lead - 1).ToList();
                    chosen.Sort();
                    for (int s = 1; s < lead; s++)
                        nightDay[s] = startDate.AddDays(chosen[s - 1]);
                }
                else
                {
                    for (int s = 0; s < lead; s++)
                        nightDay[s] = DateTime.Today.AddDays(-2 - (lead - 1 - s));
                }
            }

            for (int s = 0; s < lead; s++)
            {
                int from = sessionStart[s];
                int to = sessionStart[s + 1] - 1;
                DateTime ns = nightDay[s]
                    .AddHours(SessionStartHour)
                    .AddMinutes(rand.Next(0, NightStartJitterMinutes + 1))
                    .AddSeconds(rand.Next(0, 60));
                for (int k = from; k <= to; k++)
                    times[seq[k].Key] = ToUnix(ns.AddSeconds(relOffset[k]));
            }

            {
                int from = sessionStart[lead];
                int last = seq.Count - 1;
                long finalDuration = relOffset[last];
                DateTime platTarget = DateTime.Now.AddSeconds(-rand.Next(30, 301));
                DateTime finalStart = platTarget.AddSeconds(-finalDuration);
                for (int k = from; k <= last; k++)
                    times[seq[k].Key] = ToUnix(finalStart.AddSeconds(relOffset[k]));
            }

            bool platEarned = hasPlatinum && original.Count > 0 && original[0] != 0;
            if (platEarned)
            {
                long platOrig = original[0];
                int prevIdx = -1;
                long prevOrig = long.MinValue;
                for (int i = 1; i < original.Count; i++)
                    if (original[i] != 0 && original[i] <= platOrig && original[i] > prevOrig)
                    {
                        prevOrig = original[i];
                        prevIdx = i;
                    }
                if (prevIdx >= 0)
                    times[0] = times[prevIdx] + (platOrig - prevOrig);
            }

            long nowUnix = ToUnix(DateTime.Now);
            long maxT = times.Where(t => t != 0).Max();
            while (maxT > nowUnix)
            {
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != 0)
                        times[i] -= 24L * 3600L;
                maxT -= 24L * 3600L;
            }

            for (int i = 0; i < times.Count; i++)
                timesArr[i] = times[i];

            long firstUnlock = times.Where(t => t != 0).Min();
            long lastUnlock = times.Where(t => t != 0).Max();
            return new RelocationResult(sessions, platEarned,
                firstUnlock.TimeStampToDateTime(), lastUnlock.TimeStampToDateTime());
        }
    }
}
