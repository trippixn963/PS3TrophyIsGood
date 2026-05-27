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
    // "My PS3 Games": the linked account's PS3 library + completion.
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
            IsBusy = true;
            BusyText = "Fetching your PS3 games from PSNProfiles…";
            Ps3Library lib;
            try { lib = await Task.Run(() => PsnProfilesScraper.FetchPs3Games(MyPsnUser)); }
            catch (Exception ex) { IsBusy = false; await Modern.Info(ex.Message, "Couldn't fetch games"); return; }
            IsBusy = false;

            MyGames.Clear();
            foreach (var g in lib.Games.OrderByDescending(g => g.Percent).ThenBy(g => g.Name))
                MyGames.Add(g);
            if (MyGames.Count == 0)
            {
                await Modern.Info("No PS3 games found on your PSNProfiles account.", "My PS3 Games");
                return;
            }
            HasMyGames = true;
            SelectedTab = 2;

            // The list is up; pull banners in the background — parallel + bounded, cached ones are instant —
            // and stream each one in as it arrives instead of blocking the whole tab on the slowest download.
            var games = lib.Games.ToList();
            string cookie = lib.Cookie, ua = lib.Ua;
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            _ = Task.Run(() => System.Threading.Tasks.Parallel.ForEach(
                games,
                new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 6 },
                g =>
                {
                    var img = PsnProfilesScraper.LoadBanner(g.IconUrl, g.GameId, cookie, ua);
                    if (img != null)
                        dispatcher.BeginInvoke(new Action(() => g.Icon = img));
                }));
        }
    }
}
