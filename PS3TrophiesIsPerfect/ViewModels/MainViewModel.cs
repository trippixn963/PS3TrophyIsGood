using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Infra;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly TrophyDocument _doc = new TrophyDocument();
        private List<TrophyRow> _allRows = new List<TrophyRow>();

        public AppSettings Settings { get; } = AppSettings.Load();

        private bool _dirty;
        public bool HasUnsavedChanges => _dirty;
        private void SetDirty(bool value) { _dirty = value; Raise(nameof(WindowTitle)); }

        public string WindowTitle =>
            (_dirty ? "• " : "") + (HasGame && !string.IsNullOrEmpty(GameTitle)
                ? GameTitle + " — PS3TrophiesIsPerfect" : "PS3TrophiesIsPerfect");

        public ObservableCollection<TrophyRow> Trophies { get; } = new ObservableCollection<TrophyRow>();
        public ObservableCollection<string> Profiles { get; } = new ObservableCollection<string>();

        private string _selectedProfile = TrophyDocument.DefaultProfile;
        public string SelectedProfile
        {
            get => _selectedProfile;
            set { Set(ref _selectedProfile, value); Settings.LastProfile = value ?? ""; Settings.Save(); }
        }

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

        private string _filter = "";
        public string Filter { get => _filter; set { Set(ref _filter, value); ApplyFilter(); } }

        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ScrapeCommand { get; }
        public ICommand ClearCommand { get; }

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(Open, () => !IsBusy);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => _doc.IsOpen && !IsBusy);
            RefreshCommand = new RelayCommand(Refresh, () => _doc.IsOpen && !IsBusy);
            ScrapeCommand = new RelayCommand(async () => await ScrapeAsync(), () => _doc.IsOpen && !IsBusy);
            ClearCommand = new RelayCommand(async () => await ClearAllAsync(), () => _doc.IsOpen && !IsBusy);
        }

        /// <summary>Called once the window is loaded: start FlareSolverr and reopen the last folder.</summary>
        public async Task StartupAsync()
        {
            _ = Task.Run(() => FlareSolverr.EnsureStarted());
            LoadProfiles();
            if (!string.IsNullOrEmpty(Settings.LastFolder) && Directory.Exists(Settings.LastFolder))
                await OpenPath(Settings.LastFolder);
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
        }

        private void ApplyFilter()
        {
            Trophies.Clear();
            IEnumerable<TrophyRow> rows = _allRows;
            if (!string.IsNullOrWhiteSpace(_filter))
                rows = rows.Where(r => (r.Name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var r in rows)
                Trophies.Add(r);
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

        /// <summary>Double-click / context menu: set / change the unlock time.</summary>
        public async Task EditRow(TrophyRow row)
        {
            if (row == null || !_doc.IsOpen) return;
            if (_doc.IsSynced(row.Id))
            {
                await Modern.Info("Trophy already synchronized. Can't be modified.", "Locked");
                return;
            }
            bool got = _doc.IsGot(row.Id);
            DateTime initial = _doc.TimeOf(row.Id) ?? DateTime.Now;
            DateTime? picked = await Modern.PromptDate(got ? "Change unlock time" : "Unlock time", initial, showTime: true);
            if (picked == null) return;
            try
            {
                if (got) _doc.ChangeTime(row.Id, picked.Value);
                else _doc.Unlock(row.Id, picked.Value);
                SetDirty(true);
                Refresh();
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Can't apply");
            }
        }

        /// <summary>Context menu: lock (remove the unlock time for) a single trophy.</summary>
        public async Task LockRow(TrophyRow row)
        {
            if (row == null || !_doc.IsOpen) return;
            try
            {
                _doc.Delete(row.Id);
                SetDirty(true);
                Refresh();
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Can't lock");
            }
        }

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

            List<ScrapedTrophy> scraped;
            IsBusy = true;
            BusyText = "Scraping PSNProfiles… this can take up to a minute.";
            try
            {
                scraped = await Task.Run(() => PsnProfilesScraper.Load(url));
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await Modern.Info(ex.Message, "Scrape failed");
                return;
            }
            IsBusy = false;

            if (scraped.Count == 0)
            {
                await Modern.Info("No earned trophies were found on that page.", "Copy from PSNProfiles");
                return;
            }

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

            if (await Modern.Confirm(
                    "Rebuild this run as nightly play sessions from a start date through today, finishing " +
                    "with the platinum earned today?\n\nYes = pick the start date.   No = keep the scraped dates.",
                    "Relocate to night sessions"))
            {
                DateTime? start = await Modern.PromptDate("Start date — the first night of the run", DateTime.Today, showTime: false);
                if (start != null)
                {
                    var r = _doc.RelocateToNightSessions(times, start.Value.Date);
                    await Modern.Info(
                        $"Rebuilt across {r.Sessions} session(s) — " + (r.PlatEarned ? "platinum" : "last trophy") +
                        " earned just now.\n\nStarted:   " + r.First.ToString("yyyy/MM/dd  HH:mm:ss") +
                        "\nFinished:  " + r.Last.ToString("yyyy/MM/dd  HH:mm:ss"), "Relocate to night sessions");
                }
            }

            try
            {
                _doc.ApplyTimes(times);
                SetDirty(true);
                Refresh();
            }
            catch (Exception ex)
            {
                await Modern.Info(ex.Message, "Apply failed");
            }
        }
    }
}
