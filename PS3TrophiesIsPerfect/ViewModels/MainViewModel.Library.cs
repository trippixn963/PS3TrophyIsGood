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
    // own data (open image CDN, exact unlock times, rarity, DLC groups). Cached + shown instantly + refreshed;
    // images stream in (parallel + disk-cached forever, since completed games never change).
    public sealed partial class MainViewModel
    {
        private PsnApi _psnApi;
        private PsnApi Psn => _psnApi ?? (_psnApi = new PsnApi(Settings));

        // Full sets behind the filtered/sorted views the UI binds to.
        private List<GameProgress> _allMyGames = new List<GameProgress>();
        private List<TrophyDetail> _allTrophies = new List<TrophyDetail>();

        // Per-game trophy lists, cached for the session so re-opening a game is instant — no PSN re-fetch,
        // and the icons are already loaded on the cached objects.
        private readonly Dictionary<string, List<TrophyDetail>> _trophyCache =
            new Dictionary<string, List<TrophyDetail>>();

        public ObservableCollection<GameProgress> MyGames { get; } =
            new ObservableCollection<GameProgress>();
        public ObservableCollection<TrophyDetail> GameTrophies { get; } =
            new ObservableCollection<TrophyDetail>();

        private bool _hasMyGames;
        public bool HasMyGames
        {
            get => _hasMyGames;
            set => Set(ref _hasMyGames, value);
        }

        // ---- Games-list filter + sort ----
        public string[] LibrarySorts { get; } = { "Completion", "Name", "Recent" };
        private string _librarySort = "Completion";
        public string LibrarySort
        {
            get => _librarySort;
            set
            {
                Set(ref _librarySort, value);
                ApplyGamesView();
            }
        }

        private string _libraryFilter = "";
        public string LibraryFilter
        {
            get => _libraryFilter;
            set
            {
                Set(ref _libraryFilter, value);
                ApplyGamesView();
            }
        }

        // ---- Trophy-detail filter + sort ----
        public string[] TrophyShowOptions { get; } = { "All", "Earned", "Unearned" };
        private string _trophyShow = "All";
        public string TrophyShow
        {
            get => _trophyShow;
            set
            {
                Set(ref _trophyShow, value);
                ApplyTrophyView();
            }
        }

        public string[] TrophyDetailSorts { get; } = { "Order", "Rarest" };
        private string _trophyDetailSort = "Order";
        public string TrophyDetailSort
        {
            get => _trophyDetailSort;
            set
            {
                Set(ref _trophyDetailSort, value);
                ApplyTrophyView();
            }
        }

        // Master-detail: a selected game switches the tab from the games list to that game's trophy list.
        private GameProgress _selectedGame;
        public GameProgress SelectedGame
        {
            get => _selectedGame;
            set
            {
                Set(ref _selectedGame, value);
                Raise(nameof(IsViewingGame));
                Raise(nameof(NotViewingGame));
            }
        }
        public bool IsViewingGame => _selectedGame != null;
        public bool NotViewingGame => _selectedGame == null;

        private ICommand _backToGamesCommand;
        public ICommand BackToGamesCommand =>
            _backToGamesCommand
            ?? (
                _backToGamesCommand = new RelayCommand(() =>
                {
                    GameTrophies.Clear();
                    _allTrophies = new List<TrophyDetail>();
                    SelectedGame = null;
                })
            );

        // ---- Games list --------------------------------------------------------------------------

        private async Task LoadMyGamesAsync()
        {
            SelectedGame = null;
            SelectedTab = 2;
            var cache = Settings.MyGamesCache;
            bool validCache =
                cache != null
                && cache.Count > 0
                && cache[0].GameId != null
                && cache[0].GameId.StartsWith("NPWR", StringComparison.OrdinalIgnoreCase);
            if (validCache)
            {
                ShowGames(cache, select: true);
                StreamImages(_allMyGames);
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
            if (showBusy)
            {
                IsBusy = true;
                BusyText = "Loading your PS3 games from PlayStation…";
            }
            try
            {
                games = await Task.Run(() => Psn.GetPs3Games());
            }
            catch (PsnApi.AuthRequiredException)
            {
                // A silent background refresh must never pop the login dialog — keep showing the cache and
                // wait for an explicit action (foreground load, or opening a game) to re-link.
                if (!showBusy)
                    return;
                IsBusy = false;
                if (!await EnsureSignedInAsync())
                    return;
                IsBusy = true;
                BusyText = "Loading your PS3 games from PlayStation…";
                try
                {
                    games = await Task.Run(() => Psn.GetPs3Games());
                }
                catch (Exception ex)
                {
                    IsBusy = false;
                    await Modern.Info(ex.Message, "Couldn't load games");
                    return;
                }
            }
            catch (Exception ex)
            {
                if (showBusy)
                    IsBusy = false;
                await Modern.Info(ex.Message, "Couldn't load games");
                return;
            }
            if (showBusy)
                IsBusy = false;

            Settings.MyGamesCache = games;
            Settings.Save();

            if (games.Count == 0)
            {
                if (showBusy)
                    await Modern.Info(
                        "No PS3 games found on your PlayStation account.",
                        "My PS3 Games"
                    );
                return;
            }
            if (!SameLibrary(games))
                ShowGames(games, select: showBusy);
            StreamImages(_allMyGames);
        }

        private void ShowGames(IReadOnlyList<GameProgress> games, bool select)
        {
            _allMyGames = games.ToList();
            ApplyGamesView();
            if (select && HasMyGames)
                SelectedTab = 2;
        }

        private void ApplyGamesView()
        {
            MyGames.Clear();
            IEnumerable<GameProgress> g = _allMyGames;
            if (!string.IsNullOrWhiteSpace(_libraryFilter))
                g = g.Where(x =>
                    (x.Name ?? "").IndexOf(_libraryFilter, StringComparison.OrdinalIgnoreCase) >= 0
                );
            switch (_librarySort)
            {
                case "Name":
                    g = g.OrderBy(x => x.Name);
                    break;
                case "Recent":
                    g = g.OrderByDescending(x => x.LastUpdated).ThenBy(x => x.Name);
                    break;
                default:
                    g = g.OrderByDescending(x => x.Percent).ThenBy(x => x.Name);
                    break;
            }
            foreach (var x in g)
                MyGames.Add(x);
            HasMyGames = _allMyGames.Count > 0;
        }

        private bool SameLibrary(List<GameProgress> fresh)
        {
            if (fresh.Count != _allMyGames.Count)
                return false;
            var cur = _allMyGames.Where(g => g.GameId != null).ToDictionary(g => g.GameId);
            foreach (var g in fresh)
                if (
                    g.GameId == null
                    || !cur.TryGetValue(g.GameId, out var c)
                    || c.Earned != g.Earned
                    || c.Total != g.Total
                    || c.Percent != g.Percent
                    || c.Platinum != g.Platinum
                    || c.Gold != g.Gold
                    || c.Silver != g.Silver
                    || c.Bronze != g.Bronze
                    || c.HasDlc != g.HasDlc
                )
                    return false;
            return true;
        }

        // ---- Per-game trophy detail --------------------------------------------------------------

        public async Task OpenGameAsync(GameProgress game)
        {
            if (game == null)
                return;
            SelectedGame = game;
            GameTrophies.Clear();

            // Re-opening a game is instant: reuse the cached list (icons already loaded on those objects).
            if (game.GameId != null && _trophyCache.TryGetValue(game.GameId, out var hit))
            {
                ShowTrophies(hit, game.GameId);
                return;
            }

            IsBusy = true;
            BusyText = "Loading trophies for " + game.Name + "…";
            List<TrophyDetail> trophies;
            try
            {
                trophies = await Task.Run(() => Psn.GetTrophies(game.GameId));
            }
            catch (PsnApi.AuthRequiredException)
            {
                IsBusy = false;
                if (!await EnsureSignedInAsync())
                {
                    SelectedGame = null;
                    return;
                }
                IsBusy = true;
                BusyText = "Loading trophies for " + game.Name + "…";
                try
                {
                    trophies = await Task.Run(() => Psn.GetTrophies(game.GameId));
                }
                catch (Exception ex)
                {
                    IsBusy = false;
                    await Modern.Info(ex.Message, "Couldn't load trophies");
                    SelectedGame = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info(ex.Message, "Couldn't load trophies");
                SelectedGame = null;
                return;
            }
            IsBusy = false;

            if (game.GameId != null)
                _trophyCache[game.GameId] = trophies;
            ShowTrophies(trophies, game.GameId);
        }

        private void ShowTrophies(List<TrophyDetail> trophies, string gameId)
        {
            _allTrophies = trophies;
            _trophyShow = "All";
            Raise(nameof(TrophyShow));
            _trophyDetailSort = "Order";
            Raise(nameof(TrophyDetailSort));
            ApplyTrophyView();
            StreamTrophyIcons(_allTrophies, gameId);
        }

        private void ApplyTrophyView()
        {
            // Group the trophy list into "Base Game" / DLC sections (set once; survives Clear/Add).
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(GameTrophies);
            if (view != null && view.CanGroup && view.GroupDescriptions.Count == 0)
                view.GroupDescriptions.Add(
                    new System.Windows.Data.PropertyGroupDescription(nameof(TrophyDetail.GroupName))
                );

            GameTrophies.Clear();
            IEnumerable<TrophyDetail> t = _allTrophies;
            if (_trophyShow == "Earned")
                t = t.Where(x => x.Earned);
            else if (_trophyShow == "Unearned")
                t = t.Where(x => !x.Earned);

            // Always keep groups contiguous (base game first, then DLC) so the section headers render right;
            // order WITHIN each group by rarity or by trophy order.
            Func<TrophyDetail, int> rank = x => x.GroupId == "default" ? 0 : 1;
            var ordered = t.OrderBy(rank).ThenBy(x => x.GroupId, StringComparer.Ordinal);
            t =
                _trophyDetailSort == "Rarest"
                    ? ordered
                        .ThenBy(x => x.EarnedRate <= 0 ? double.MaxValue : x.EarnedRate)
                        .ThenBy(x => x.Id)
                    : ordered.ThenBy(x => x.Id);

            foreach (var x in t)
                GameTrophies.Add(x);
        }

        // ---- Auth + art ---------------------------------------------------------------------------

        private async Task<bool> EnsureSignedInAsync()
        {
            string npsso = await Modern.PromptNpsso();
            if (string.IsNullOrWhiteSpace(npsso))
                return false;
            IsBusy = true;
            BusyText = "Linking your PlayStation account…";
            try
            {
                await Task.Run(() => Psn.SignIn(npsso));
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info(
                    "Couldn't link your account — the token may be wrong or expired.\n\n"
                        + ex.Message,
                    "PlayStation link failed"
                );
                return false;
            }
        }

        private static void StreamImages(IReadOnlyList<GameProgress> games)
        {
            var disp = Application.Current.Dispatcher;
            var list = games.ToList();
            _ = Task.Run(() =>
                System.Threading.Tasks.Parallel.ForEach(
                    list,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                    g =>
                    {
                        if (g.Icon != null)
                            return;
                        var img = ImageCache.Get(g.IconUrl, g.GameId);
                        if (img != null)
                            disp.BeginInvoke(new Action(() => g.Icon = img));
                    }
                )
            );
        }

        private static void StreamTrophyIcons(IReadOnlyList<TrophyDetail> trophies, string gameId)
        {
            var disp = Application.Current.Dispatcher;
            var list = trophies.ToList();
            _ = Task.Run(() =>
                System.Threading.Tasks.Parallel.ForEach(
                    list,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                    t =>
                    {
                        if (t.Icon != null)
                            return;
                        var img = ImageCache.Get(t.IconUrl, gameId + "_" + t.Id);
                        if (img != null)
                            disp.BeginInvoke(new Action(() => t.Icon = img));
                    }
                )
            );
        }
    }
}
