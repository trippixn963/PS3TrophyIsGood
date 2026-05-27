using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PS3TrophiesIsPerfect.Dialogs;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // Pre-sync legitimacy check: scans the loaded run for the things PSN actually flags, so problems are
    // caught before syncing rather than after a ban. Critical = impossible/instant-flag; warning = suspicious.
    public sealed partial class MainViewModel
    {
        private async Task RunCheckAsync()
        {
            if (!_doc.IsOpen)
                return;

            var crit = new List<string>();
            var warn = new List<string>();
            var got = _allRows.Where(r => r.Got && r.Time.HasValue).ToList();
            DateTime now = DateTime.Now;

            // Future-dated unlocks are an instant flag on sync.
            var future = got.Where(r => r.Time.Value > now).ToList();
            if (future.Count > 0)
                crit.Add(
                    $"{future.Count} trophy(ies) are dated in the future — an instant flag "
                        + $"(latest {future.Max(r => r.Time.Value):yyyy/MM/dd HH:mm})."
                );

            var plat = _allRows.FirstOrDefault(r => r.Type == "P");
            if (plat != null && plat.Got && plat.Time.HasValue)
            {
                // The platinum can't pre-date the trophies that earn it.
                var others = got.Where(r => r.Type != "P").ToList();
                if (others.Count > 0)
                {
                    DateTime last = others.Max(r => r.Time.Value);
                    if (plat.Time.Value < last)
                        crit.Add(
                            "The platinum is timed before the last trophy it requires "
                                + $"({plat.Time.Value:yyyy/MM/dd HH:mm} is earlier than {last:yyyy/MM/dd HH:mm})."
                        );
                }

                // A platinum can't exist while any other trophy is still locked.
                int locked = _allRows.Count(r => r.Type != "P" && !r.Got);
                if (locked > 0)
                    crit.Add(
                        $"The platinum is earned but {locked} other trophy(ies) are still locked — impossible."
                    );
            }

            // Many trophies on the exact same second read as copy-pasted timestamps.
            var clusters = got.GroupBy(r => r.Time.Value).Where(g => g.Count() >= 4).ToList();
            if (clusters.Count > 0)
                warn.Add(
                    $"{clusters.Sum(g => g.Count())} trophies share only {clusters.Count} identical "
                        + "timestamp(s) — large exact-time clusters can look copy-pasted."
                );

            // Relocation never goes faster than the donor; if the comparison says it did, something's off.
            if (HasDonor && Comparison != null)
            {
                int faster = Comparison.Count(c => c.Match == "faster");
                if (faster > 0)
                    warn.Add(
                        $"{faster} gap(s) are faster than the donor's — re-run Relocate to fix the pacing."
                    );
            }

            await Modern.CheckReport(crit.Count == 0 && warn.Count == 0, crit, warn);
        }
    }
}
