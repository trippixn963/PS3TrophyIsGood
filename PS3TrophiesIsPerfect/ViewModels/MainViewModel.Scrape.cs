using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // The "Copy from PSNProfiles" workflow: scrape -> match -> offer relocation -> apply.
    public sealed partial class MainViewModel
    {
        private async Task ScrapeAsync()
        {
            string url = await Modern.PromptUrl();
            if (url == null) return;
            if (!PsnProfilesScraper.LooksLikeTrophyUrl(url))
            {
                await Modern.Info("Enter a PSNProfiles game-trophy URL, e.g.\n" +
                    "https://psnprofiles.com/trophies/41027-pragmata/SomeUser", "Copy from PSNProfiles");
                return;
            }

            ScrapeResult result;
            IsBusy = true;
            BusyText = "Scraping PSNProfiles… this can take up to a minute.";
            try
            {
                result = await Task.Run(() => PsnProfilesScraper.Load(url));
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info(ex.Message, "Scrape failed");
                return;
            }
            IsBusy = false;

            var scraped = result.Trophies;
            if (scraped.Count == 0)
            {
                await Modern.Info("No earned trophies were found on that page.", "Copy from PSNProfiles");
                return;
            }

            // Populate the comparison panel with the donor's own list (their order + times) + identity.
            DonorUser = UserFromUrl(url);
            DonorAvatarUrl = result.AvatarUrl ?? "";
            var donorEntries = scraped.Select(s => new DonorEntry { Name = s.Name, Date = s.Date }).ToList();
            Settings.Donor = donorEntries;
            Settings.DonorUser = DonorUser;
            Settings.DonorAvatarUrl = DonorAvatarUrl;
            Settings.DonorTitle = "Cloned from " + DonorUser;
            Settings.Save();
            ShowDonor(donorEntries, Settings.DonorTitle);

            long[] times = _doc.MatchScrape(scraped, out int matched, out List<string> unmatched);
            if (unmatched.Count > 0)
            {
                const int max = 15;
                string list = string.Join("\n  • ", unmatched.Take(max));
                if (unmatched.Count > max) list += $"\n  … and {unmatched.Count - max} more";
                await Modern.Info(
                    $"Matched {matched} of {matched + unmatched.Count} scraped trophies by name.\n\n" +
                    "These matched no trophy and were skipped:\n  • " + list, "Copy from PSNProfiles");
            }

            if (!times.Any(t => t != 0))
                return;

            bool relocated = false;
            RelocationResult r = default;
            if (await Modern.Confirm(
                    "Rebuild this run as nightly play sessions from a start date through today, finishing " +
                    "with the platinum earned today?\n\nYes = pick the start date.   No = keep the scraped dates.",
                    "Relocate to night sessions"))
            {
                DateTime? start = await Modern.PromptDate("Start date — the first night of the run", DateTime.Today, showTime: false);
                if (start != null)
                {
                    r = _doc.RelocateToNightSessions(times, start.Value.Date);
                    relocated = true;
                }
            }

            try
            {
                _doc.ApplyTimes(times);
                SetDirty(true);
                Refresh();
                ShowToast(relocated
                    ? $"Relocated: {r.Sessions} session(s), {(r.PlatEarned ? "platinum" : "last trophy")} today"
                    : $"Applied {matched} scraped trophies");
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Apply failed");
            }
        }

        /// <summary>Re-runs (re-rolls) the night-session relocation on the already-scraped donor, with a
        /// freshly chosen start date — no need to scrape the page again.</summary>
        private async Task RelocateAsync()
        {
            if (!_doc.IsOpen || Settings.Donor == null || Settings.Donor.Count == 0)
            {
                await Modern.Info("Copy a donor's trophies from PSNProfiles first.", "Relocate");
                return;
            }

            var scraped = Settings.Donor.Select(d => new ScrapedTrophy(d.Date, d.Name)).ToList();
            long[] times = _doc.MatchScrape(scraped, out int matched, out _);
            if (matched == 0 || !times.Any(t => t != 0))
            {
                await Modern.Info("None of the donor's trophies match the open game.", "Relocate");
                return;
            }

            DateTime? start = await Modern.PromptDate("Start date — the first night of the run", DateTime.Today, showTime: false);
            if (start == null) return;

            var r = _doc.RelocateToNightSessions(times, start.Value.Date);
            try
            {
                _doc.ApplyTimes(times);
                SetDirty(true);
                Refresh();
                ShowToast($"Relocated: {r.Sessions} session(s), {(r.PlatEarned ? "platinum" : "last trophy")} today");
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Apply failed");
            }
        }

        private static string UserFromUrl(string url)
        {
            try
            {
                var seg = url.Split('?')[0].TrimEnd('/').Split('/');
                return seg.Length > 0 ? seg[seg.Length - 1] : "";
            }
            catch { return ""; }
        }
    }
}
