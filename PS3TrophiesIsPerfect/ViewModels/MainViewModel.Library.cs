using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Infra;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // "My PS3 Games": the linked PlayStation account's PS3 library + per-game trophy detail, from Sony's
    // own data (open image CDN, exact unlock times). The list is cached and shown instantly, then refreshed;
    // banners and trophy icons stream in (parallel + disk-cached forever, since completed games never change).
    public sealed partial class MainViewModel
    {
        private PsnApi _psnApi;
        private PsnApi Psn => _psnApi ?? (_psnApi = new PsnApi(Settings));

        public ObservableCollection<GameProgress> MyGames { get; } = new ObservableCollection<GameProgress>();
        public ObservableCollection<TrophyDetail> GameTrophies { get; } = new ObservableCollection<TrophyDetail>();

        private bool _hasMyGames;
        public bool HasMyGames { get => _hasMyGames; set => Set(ref _hasMyGames, value); }

        // Master-detail: a selected game switches the tab from the games list to that game's trophy list.
        private GameProgress _selectedGame;
        public GameProgress SelectedGame
        {
            get => _selectedGame;
            set { Set(ref _selectedGame, value); Raise(nameof(IsViewingGame)); Raise(nameof(NotViewingGame)); }
        }
        public bool IsViewingGame => _selectedGame != null;
        public bool NotViewingGame => _selectedGame == null;

        private ICommand _backToGamesCommand;
        public ICommand BackToGamesCommand => _backToGamesCommand ??
            (_backToGamesCommand = new RelayCommand(() => { GameTrophies.Clear(); SelectedGame = null; }));

        // ---- Games list --------------------------------------------------------------------------

        private async Task LoadMyGamesAsync()
        {
            SelectedGame = null;
            SelectedTab = 2;
            // Only trust a PSN-shaped cache (npCommunicationIds look like "NPWR…"); ignore any old
            // PSNProfiles-format cache from before this feature moved to Sony's data.
            var cache = Settings.MyGamesCache;
            bool validCache = cache != null && cache.Count > 0
                && cache[0].GameId != null && cache[0].GameId.StartsWith("NPWR", StringComparison.OrdinalIgnoreCase);
            if (validCache)
            {
                // Instant: show the cached list + cached art (no round-trip), then refresh quietly.
                ShowGames(cache, select: true);
                StreamImages(MyGames.ToList());
                _ = RefreshGamesLive(showBusy: false);
            }
            else
            {
                await RefreshGamesLive(showBusy: true);
            }
        }

        private async Task RefreshGamesLive(bool showBusy)
        {
            List<GameProgress> games;
            if (showBusy) { IsBusy = true; BusyText = "Loading your PS3 games from PlayStation…"; }
            try { games = await Task.Run(() => Psn.GetPs3Games()); }
            catch (PsnApi.AuthRequiredException)
            {
                if (showBusy) IsBusy = false;
                if (!await EnsureSignedInAsync()) return; // keep showing whatever we already had
                if (showBusy) { IsBusy = true; BusyText = "Loading your PS3 games from PlayStation…"; }
                try { games = await Task.Run(() => Psn.GetPs3Games()); }
                catch (Exception ex) { IsBusy = false; await Modern.Info(ex.Message, "Couldn't load games"); return; }
            }
            catch (Exception ex) { if (showBusy) IsBusy = false; await Modern.Info(ex.Message, "Couldn't load games"); return; }
            if (showBusy) IsBusy = false;

            // Cache forever — completed games are immutable.
            Settings.MyGamesCache = games;
            Settings.Save();

            if (games.Count == 0)
            {
                if (showBusy) await Modern.Info("No PS3 games found on your PlayStation account.", "My PS3 Games");
                return;
            }
            if (!SameLibrary(games))
                ShowGames(games, select: showBusy);
            StreamImages(MyGames.ToList());
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
                    || c.Earned != g.Earned || c.Total != g.Total || c.Percent != g.Percent
                    || c.Platinum != g.Platinum || c.Gold != g.Gold || c.Silver != g.Silver
                    || c.Bronze != g.Bronze || c.HasDlc != g.HasDlc)
                    return false;
            return true;
        }

        // ---- Per-game trophy detail --------------------------------------------------------------

        /// <summary>Opens a game's trophy list (icons, descriptions, types, unlock times). Called from the view.</summary>
        public async Task OpenGameAsync(GameProgress game)
        {
            if (game == null) return;
            SelectedGame = game;
            GameTrophies.Clear();

            IsBusy = true;
            BusyText = "Loading trophies for " + game.Name + "…";
            List<TrophyDetail> trophies;
            try { trophies = await Task.Run(() => Psn.GetTrophies(game.GameId)); }
            catch (PsnApi.AuthRequiredException)
            {
                IsBusy = false;
                if (!await EnsureSignedInAsync()) { SelectedGame = null; return; }
                IsBusy = true; BusyText = "Loading trophies for " + game.Name + "…";
                try { trophies = await Task.Run(() => Psn.GetTrophies(game.GameId)); }
                catch (Exception ex) { IsBusy = false; await Modern.Info(ex.Message, "Couldn't load trophies"); SelectedGame = null; return; }
            }
            catch (Exception ex) { IsBusy = false; await Modern.Info(ex.Message, "Couldn't load trophies"); SelectedGame = null; return; }
            IsBusy = false;

            foreach (var t in trophies)
                GameTrophies.Add(t);
            StreamTrophyIcons(trophies, game.GameId);
        }

        // ---- Auth + art ---------------------------------------------------------------------------

        private async Task<bool> EnsureSignedInAsync()
        {
            string npsso = await Modern.PromptNpsso();
            if (string.IsNullOrWhiteSpace(npsso)) return false;
            IsBusy = true;
            BusyText = "Linking your PlayStation account…";
            try { await Task.Run(() => Psn.SignIn(npsso)); IsBusy = false; return true; }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info("Couldn't link your account — the token may be wrong or expired.\n\n" + ex.Message,
                    "PlayStation link failed");
                return false;
            }
        }

        private static void StreamImages(IReadOnlyList<GameProgress> games)
        {
            var disp = Application.Current.Dispatcher;
            var list = games.ToList();
            _ = Task.Run(() => System.Threading.Tasks.Parallel.ForEach(
                list, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                g =>
                {
                    if (g.Icon != null) return;
                    var img = ImageCache.Get(g.IconUrl, g.GameId);
                    if (img != null) disp.BeginInvoke(new Action(() => g.Icon = img));
                }));
        }

        private static void StreamTrophyIcons(IReadOnlyList<TrophyDetail> trophies, string gameId)
        {
            var disp = Application.Current.Dispatcher;
            var list = trophies.ToList();
            _ = Task.Run(() => System.Threading.Tasks.Parallel.ForEach(
                list, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                t =>
                {
                    var img = ImageCache.Get(t.IconUrl, gameId + "_" + t.Id);
                    if (img != null) disp.BeginInvoke(new Action(() => t.Icon = img));
                }));
        }
    }
}
