using System;
using System.Collections.Generic;
using System.Linq;

namespace PS3TrophiesIsPerfect.Services
{
    public struct RelocationResult
    {
        public int Sessions;
        public bool PlatEarned;
        public DateTime First,
            Last;

        public RelocationResult(int sessions, bool platEarned, DateTime first, DateTime last)
        {
            Sessions = sessions;
            PlatEarned = platEarned;
            First = first;
            Last = last;
        }
    }

    /// <summary>
    /// Rebuilds an imported unlock sequence as realistic nightly play sessions that SPAN the window the user
    /// picked: the first trophy lands on <paramref name="startDate"/> and the platinum pops ~now, with the
    /// sessions stretched proportionally across that span (a short window → a few long sessions; a long
    /// window → sessions spread across the days). HARD INVARIANTS (do not change): bursts (≤60s) keep the
    /// donor's EXACT gaps; the platinum keeps the donor's EXACT pop-gap; every other in-session gap is the
    /// donor's plus a few minutes (always SLOWER, never faster); nothing is ever dated in the future; the
    /// first trophy is never before the chosen start. Mutates <paramref name="timesArr"/>.
    /// </summary>
    public static class RelocationEngine
    {
        public static RelocationResult Rebuild(
            long[] timesArr,
            DateTime startDate,
            bool hasPlatinum
        )
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
            var original = new List<long>(times);

            if (startDate.Date > DateTime.Today)
                startDate = DateTime.Today;
            startDate = startDate.Date;

            // Donor trophies (incl. the platinum at index 0), in unlock order.
            var seq = new List<KeyValuePair<int, long>>();
            for (int i = 0; i < original.Count; i++)
                if (original[i] != 0)
                    seq.Add(new KeyValuePair<int, long>(i, original[i]));
            seq.Sort((a, b) => a.Value.CompareTo(b.Value));
            int n = seq.Count;
            if (n == 0)
                return new RelocationResult(0, false, DateTime.MinValue, DateTime.MinValue);

            var rand = new Random();
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ToUnix(DateTime dt) => (long)(dt - epoch).TotalSeconds;

            // Never date the run before it was provably earnable. The donor is a real player, so their
            // earliest unlock is on/after the game's release and after their account existed — a safe floor.
            // Clamp the chosen start up to it so no trophy can ever predate the game (the #1 instant tell).
            DateTime earnableFloor = epoch.AddSeconds(seq[0].Value).Date;
            if (earnableFloor > DateTime.Today)
                earnableFloor = DateTime.Today;
            if (startDate < earnableFloor)
                startDate = earnableFloor;

            // ---- Phase 1: per-step "play time" between consecutive trophies (a continuous timeline). -------
            // Bursts keep the donor's exact gap; everything else is the donor's gap plus a few minutes.
            var add = new long[n];
            var isBurst = new bool[n];
            for (int k = 1; k < n; k++)
            {
                long gap = seq[k].Value - seq[k - 1].Value;
                if (gap <= BurstGapSeconds)
                {
                    isBurst[k] = true;
                    add[k] = gap;
                }
                else
                {
                    long extraMin = rand.Next(MinExtraMinutes, MaxExtraMinutes + 1);
                    if (rand.Next(100) < LullChancePercent)
                        extraMin += rand.Next(LullMinMinutes, LullMaxMinutes + 1);
                    add[k] = gap + extraMin * 60 + rand.Next(0, 60);
                }
            }

            // ---- Phase 2: how many sessions would the donor's pacing naturally produce? --------------------
            int naturalSessions = 1;
            {
                long elapsed = 0;
                long nightLenSec = (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
                for (int k = 1; k < n; k++)
                {
                    if (!isBurst[k] && elapsed + add[k] > nightLenSec)
                    {
                        naturalSessions++;
                        elapsed = 0;
                        nightLenSec =
                            (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
                    }
                    else
                        elapsed += add[k];
                }
            }

            // ---- Phase 3: fit the sessions to the chosen window. -------------------------------------------
            int windowDays = Math.Max(0, (int)(DateTime.Today - startDate).TotalDays);
            int availableNights = windowDays + 1;
            int minSessions = windowDays >= 1 ? 2 : 1; // need one on the start day AND one today when the window spans days
            int targetSessions = Math.Max(minSessions, Math.Min(naturalSessions, availableNights));
            targetSessions = Math.Min(targetSessions, n); // can't have more sessions than trophies

            // Break the sequence into exactly `targetSessions` sessions at the largest non-burst gaps (never
            // splitting a burst, and never splitting immediately before the platinum so its pop-gap is kept).
            var breaks = new List<int>();
            if (targetSessions > 1)
            {
                var candidates = new List<KeyValuePair<int, long>>();
                for (int k = 1; k < n; k++)
                    if (!isBurst[k] && seq[k].Key != 0)
                        candidates.Add(new KeyValuePair<int, long>(k, add[k]));
                candidates.Sort((a, b) => b.Value.CompareTo(a.Value)); // largest gaps first
                foreach (var c in candidates.Take(targetSessions - 1))
                    breaks.Add(c.Key);
                breaks.Sort();
            }

            var sessionStart = new List<int> { 0 };
            sessionStart.AddRange(breaks);
            int sessions = sessionStart.Count;

            // Relative offset of each trophy from the start of ITS session.
            var relOffset = new long[n];
            for (int s = 0; s < sessions; s++)
            {
                int from = sessionStart[s];
                int to = (s + 1 < sessions ? sessionStart[s + 1] : n) - 1;
                relOffset[from] = 0;
                for (int k = from + 1; k <= to; k++)
                    relOffset[k] = relOffset[k - 1] + add[k];
            }

            // ---- Phase 4: assign each session a day, stretched across [startDate .. today]. ----------------
            var nightDay = new DateTime[sessions];
            if (sessions == 1)
            {
                nightDay[0] = startDate; // window is a single day → anchored to now below
            }
            else
            {
                for (int s = 0; s < sessions; s++)
                {
                    int dayOffset = (int)Math.Round((double)s / (sessions - 1) * windowDays);
                    nightDay[s] = startDate.AddDays(dayOffset);
                }
                nightDay[0] = startDate; // first trophy is exactly on the chosen start
                nightDay[sessions - 1] = DateTime.Today; // last session is today (platinum pops now)
                for (int s = 1; s < sessions; s++) // keep days strictly increasing
                    if (nightDay[s] <= nightDay[s - 1])
                        nightDay[s] = nightDay[s - 1].AddDays(1);
            }

            // ---- Phase 5: lay the trophies onto the timeline. ----------------------------------------------
            // Every session but the last starts in the evening of its night; the last is anchored so the
            // platinum lands ~now.
            for (int s = 0; s < sessions - 1; s++)
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
                int from = sessionStart[sessions - 1];
                int last = n - 1;
                long finalDuration = relOffset[last];
                DateTime platTarget = DateTime.Now.AddSeconds(-rand.Next(30, 301));
                DateTime finalStart = platTarget.AddSeconds(-finalDuration);
                for (int k = from; k <= last; k++)
                    times[seq[k].Key] = ToUnix(finalStart.AddSeconds(relOffset[k]));
            }

            // ---- Phase 6: pin the platinum to the donor's EXACT pop-gap after its predecessor. -------------
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

            // ---- Phase 7: never the future. Shift the whole run back by just the overflow (keeps it today). -
            long nowUnix = ToUnix(DateTime.Now);
            long maxT = times.Where(t => t != 0).Max();
            if (maxT > nowUnix)
            {
                long shift = maxT - nowUnix;
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != 0)
                        times[i] -= shift;
            }

            for (int i = 0; i < times.Count; i++)
                timesArr[i] = times[i];

            long firstUnlock = times.Where(t => t != 0).Min();
            long lastUnlock = times.Where(t => t != 0).Max();
            return new RelocationResult(
                sessions,
                platEarned,
                firstUnlock.TimeStampToDateTime(),
                lastUnlock.TimeStampToDateTime()
            );
        }
    }
}
