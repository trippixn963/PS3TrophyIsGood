using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Infra;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    /// <summary>
    /// The window's view model. Split across partial files by concern:
    ///   MainViewModel.cs            — shell: document, open/save/refresh, hero, busy, profiles, commands
    ///   MainViewModel.Trophies.cs   — the trophy grid (rows, filter, edit/lock)
    ///   MainViewModel.Scrape.cs     — the PSNProfiles "Copy from" workflow
    ///   MainViewModel.Comparison.cs — the donor-vs-you comparison
    ///   MainViewModel.Library.cs    — "My PS3 Games"
    ///   MainViewModel.Account.cs    — the linked PSN account
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject
    {
        private readonly TrophyDocument _doc = new TrophyDocument();

        public AppSettings Settings { get; } = AppSettings.Load();

        private bool _dirty;
        public bool HasUnsavedChanges => _dirty;
        private void SetDirty(bool value) { _dirty = value; Raise(nameof(WindowTitle)); }

        public string WindowTitle =>
            (_dirty ? "• " : "") + (HasGame && !string.IsNullOrEmpty(GameTitle)
                ? GameTitle + " — PS3TrophiesIsPerfect" : "PS3TrophiesIsPerfect");

        public ObservableCollection<string> Profiles { get; } = new ObservableCollection<string>();

        private string _selectedProfile = TrophyDocument.DefaultProfile;
        public string SelectedProfile
        {
            get => _selectedProfile;
            set { Set(ref _selectedProfile, value); Settings.LastProfile = value ?? ""; Settings.Save(); }
        }

        private int _selectedTab;
        public int SelectedTab { get => _selectedTab; set => Set(ref _selectedTab, value); }

        private string _gameTitle = "No game loaded";
        public string GameTitle { get => _gameTitle; set { Set(ref _gameTitle, value); Raise(nameof(WindowTitle)); } }

        private string _gameSubtitle = "Open a trophy folder, or drag one here";
        public string GameSubtitle { get => _gameSubtitle; set => Set(ref _gameSubtitle, value); }

        private int _completionPercent;
        public int CompletionPercent { get => _completionPercent; set { Set(ref _completionPercent, value); Raise(nameof(CompletionText)); } }
        public string CompletionText => CompletionPercent + "%";

        private System.Windows.Media.ImageSource _gameIcon;
        public System.Windows.Media.ImageSource GameIcon { get => _gameIcon; set => Set(ref _gameIcon, value); }

        private bool _hasGame;
        public bool HasGame { get => _hasGame; set { Set(ref _hasGame, value); Raise(nameof(EmptyHintVisible)); Raise(nameof(WindowTitle)); } }
        public bool EmptyHintVisible => !HasGame;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }

        private string _busyText = "Working…";
        public string BusyText { get => _busyText; set => Set(ref _busyText, value); }

        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ScrapeCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearDonorCommand { get; }
        public ICommand SetMyUserCommand { get; }
        public ICommand MyGamesCommand { get; }

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(Open, () => !IsBusy);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => _doc.IsOpen && !IsBusy);
            RefreshCommand = new RelayCommand(Refresh, () => _doc.IsOpen && !IsBusy);
            ScrapeCommand = new RelayCommand(async () => await ScrapeAsync(), () => _doc.IsOpen && !IsBusy);
            ClearCommand = new RelayCommand(async () => await ClearAllAsync(), () => _doc.IsOpen && !IsBusy);
            ClearDonorCommand = new RelayCommand(ClearDonor);
            SetMyUserCommand = new RelayCommand(async () => await SetMyUserAsync(), () => !IsBusy);
            MyGamesCommand = new RelayCommand(async () => await LoadMyGamesAsync(), () => !IsBusy);
        }

        /// <summary>Called once the window is loaded: start FlareSolverr and reopen the last folder.</summary>
        public async Task StartupAsync()
        {
            _ = Task.Run(() => FlareSolverr.EnsureStarted());
            MyPsnUser = Settings.MyPsnUser ?? "";
            MyAvatarUrl = Settings.MyAvatarUrl ?? "";
            LoadProfiles();
            if (!string.IsNullOrEmpty(Settings.LastFolder) && Directory.Exists(Settings.LastFolder))
                await OpenPath(Settings.LastFolder);
            if (Settings.Donor != null && Settings.Donor.Count > 0)
            {
                DonorUser = Settings.DonorUser ?? "";
                DonorAvatarUrl = Settings.DonorAvatarUrl ?? "";
                ShowDonor(Settings.Donor, Settings.DonorTitle);
            }
        }

        private void LoadProfiles()
        {
            Profiles.Clear();
            Profiles.Add(TrophyDocument.DefaultProfile);
            try
            {
                Directory.CreateDirectory("profiles");
                foreach (var f in new DirectoryInfo("profiles").GetFiles("*.sfo"))
                    Profiles.Add(f.Name);
            }
            catch { /* no profiles dir → just Default */ }
            SelectedProfile = Profiles.Contains(Settings.LastProfile) ? Settings.LastProfile : TrophyDocument.DefaultProfile;
        }

        public void Open()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                _ = OpenPath(dlg.SelectedPath);
            }
        }

        public async Task OpenPath(string folder)
        {
            IsBusy = true;
            BusyText = "Opening trophy folder…";
            try
            {
                await Task.Run(() => _doc.Open(folder));
                Refresh();
                SetDirty(false);
                Settings.LastFolder = folder;
                Settings.Save();
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Open failed");
            }
            finally { IsBusy = false; }
        }

        public void Refresh()
        {
            if (!_doc.IsOpen) return;
            _allRows = _doc.BuildRows();
            ApplyFilter();

            var s = _doc.Stats();
            GameTitle = string.IsNullOrEmpty(s.Title) ? "(untitled)" : s.Title;
            GameSubtitle = $"{s.Got} / {s.Total} trophies      {s.GetGrade} / {s.TotalGrade} pts";
            CompletionPercent = s.Percent;
            GameIcon = _doc.LoadGameIcon();
            HasGame = true;
            if (HasDonor) RebuildComparison();
        }

        public async Task SaveAsync(bool notify = true)
        {
            IsBusy = true;
            BusyText = "Saving…";
            try
            {
                string profile = SelectedProfile ?? TrophyDocument.DefaultProfile;
                await Task.Run(() => _doc.Save(profile));
                SetDirty(false);
                IsBusy = false;
                if (notify) await Modern.Info("Saved.");
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info(ex.Message, "Save failed");
            }
        }

        private async Task ClearAllAsync()
        {
            if (!await Modern.Confirm("Lock every trophy (clear all unlock times)?", "Clear trophies"))
                return;
            _doc.ClearAll();
            SetDirty(true);
            Refresh();
        }
    }
}
