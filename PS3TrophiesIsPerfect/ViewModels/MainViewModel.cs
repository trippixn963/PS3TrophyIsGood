using System.Collections.ObjectModel;
using PS3TrophiesIsPerfect.Models;

namespace PS3TrophiesIsPerfect.ViewModels
{
    /// <summary>
    /// Backing model for the main window. For now it carries representative sample data so the
    /// Fluent shell can be evaluated visually; the real loader (wrapping the frozen TROPHYParser)
    /// will replace <see cref="LoadSample"/>.
    /// </summary>
    public sealed class MainViewModel : ObservableObject
    {
        public ObservableCollection<TrophyRow> Trophies { get; } = new ObservableCollection<TrophyRow>();

        private string _gameTitle = "No game loaded";
        public string GameTitle { get => _gameTitle; set => Set(ref _gameTitle, value); }

        private string _gameSubtitle = "Open a trophy folder, or drag one here";
        public string GameSubtitle { get => _gameSubtitle; set => Set(ref _gameSubtitle, value); }

        private int _completionPercent;
        public int CompletionPercent { get => _completionPercent; set { Set(ref _completionPercent, value); Raise(nameof(CompletionText)); } }

        public string CompletionText => CompletionPercent + "%";

        public MainViewModel()
        {
            LoadSample();
        }

        private void LoadSample()
        {
            GameTitle = "Super Stardust HD";
            CompletionPercent = 100;
            GameSubtitle = "24 / 24 trophies      1230 / 1230 pts";

            Trophies.Add(new TrophyRow { Name = "Super Stardust HD", Detail = "Earn every trophy", Type = "P", Got = true, Synced = false, Time = "16 Jul 2008  01:50", Elapsed = "3d 4h 12m (+1s)" });
            Trophies.Add(new TrophyRow { Name = "Survivor", Detail = "Survive for 5 minutes", Type = "G", Got = true, Synced = false, Time = "13 Jul 2008  22:14", Elapsed = "" });
            Trophies.Add(new TrophyRow { Name = "Bomb Disposal", Detail = "Clear a wave using only bombs", Type = "S", Got = true, Synced = true, Time = "13 Jul 2008  22:41", Elapsed = "27m 3s (+27m 3s)" });
            Trophies.Add(new TrophyRow { Name = "Sharpshooter", Detail = "Reach a x10 multiplier", Type = "S", Got = true, Synced = false, Time = "14 Jul 2008  00:02", Elapsed = "1h 48m (+1h 21m)" });
            Trophies.Add(new TrophyRow { Name = "First Steps", Detail = "Finish the tutorial", Type = "B", Got = true, Synced = false, Time = "13 Jul 2008  22:09", Elapsed = "" });
            Trophies.Add(new TrophyRow { Name = "Planet Cleared", Detail = "Clear an entire planet", Type = "B", Got = true, Synced = false, Time = "15 Jul 2008  23:30", Elapsed = "2d 1h 21m (+12m)" });
            Trophies.Add(new TrophyRow { Name = "Untouchable", Detail = "Clear a wave without dying", Type = "G", Got = false, Synced = false, Time = "", Elapsed = "" });
        }
    }
}
