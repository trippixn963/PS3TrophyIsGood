using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // "My PS3 Games": the linked account's PS3 library + completion. Cached (completed games never change),
    // shown instantly, then refreshed in the background; banners stream in (parallel + disk-cached).
    public sealed partial class MainViewModel
    {
        public ObservableCollection<GameProgress> MyGames { get; } = new ObservableCollection<GameProgress>();

        private bool _hasMyGames;
        public bool HasMyGames { get => _hasMyGames; set => Set(ref _hasMyGames, value); }

        private async Task LoadMyGamesAsync()
        {
            if (!HasMyUser)
            {
                await SetMyUserAsync();
                if (!HasMyUser) return;
            }

            bool hadCache = Settings.MyGamesUser == MyPsnUser
                && Settings.MyGamesCache != null && Settings.MyGamesCache.Count > 0;
            if (hadCache)
            {
                // Instant: show the cached list (no Cloudflare wait) + any cached banners, then refresh quietly.
                ShowGames(Settings.MyGamesCache, select: true);
                StreamBanners(MyGames.ToList(), cookie: null, ua: null);
                _ = RefreshGamesLive(showBusy: false);
            }
            else
            {
                await RefreshGamesLive(showBusy: true);
            }
        }

        private async Task RefreshGamesLive(bool showBusy)
        {
            if (showBusy) { IsBusy = true; BusyText = "Fetching your PS3 games from PSNProfiles…"; }
            Ps3Library lib;
            try { lib = await Task.Run(() => PsnProfilesScraper.FetchPs3Games(MyPsnUser)); }
            catch (Exception ex)
            {
                if (showBusy) { IsBusy = false; await Modern.Info(ex.Message, "Couldn't fetch games"); }
                return; // keep showing whatever we already had
            }
            if (showBusy) IsBusy = false;

            // Persist the fresh library — completed games are immutable, so this cache is safe to keep.
            Settings.MyGamesUser = MyPsnUser;
            Settings.MyGamesCache = lib.Games;
            Settings.Save();

            if (lib.Games.Count == 0)
            {
                if (showBusy) await Modern.Info("No PS3 games found on your PSNProfiles account.", "My PS3 Games");
                return;
            }

            // Only rebuild the list if something actually changed (avoids flicker on the common no-op refresh);
            // either way, fill in any still-missing banners on the rows now on screen using the fresh cookie.
            if (!SameLibrary(lib.Games))
                ShowGames(lib.Games, select: showBusy);
            StreamBanners(MyGames.ToList(), lib.Cookie, lib.Ua);
        }

        private void ShowGames(IReadOnlyList<GameProgress> games, bool select)
        {
            MyGames.Clear();
            foreach (var g in games.OrderByDescending(g => g.Percent).ThenBy(g => g.Name))
                MyGames.Add(g);
            HasMyGames = MyGames.Count > 0;
            if (select && HasMyGames) SelectedTab = 2;
        }

        private bool SameLibrary(List<GameProgress> fresh)
        {
            if (fresh.Count != MyGames.Count) return false;
            var cur = MyGames.Where(g => g.GameId != null).ToDictionary(g => g.GameId);
            foreach (var g in fresh)
                if (g.GameId == null || !cur.TryGetValue(g.GameId, out var c)
                    || c.Earned != g.Earned || c.Total != g.Total || c.Percent != g.Percent)
                    return false;
            return true;
        }

        private static void StreamBanners(IReadOnlyList<GameProgress> games, string cookie, string ua)
        {
            var disp = System.Windows.Application.Current.Dispatcher;
            var list = games.ToList();
            _ = Task.Run(() => System.Threading.Tasks.Parallel.ForEach(
                list,
                new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                g =>
                {
                    if (g.Icon != null) return; // already shown
                    var img = PsnProfilesScraper.LoadBanner(g.IconUrl, g.GameId, cookie, ua);
                    if (img != null) disp.BeginInvoke(new Action(() => g.Icon = img));
                }));
        }
    }
}
