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
    public sealed class MainViewModel : ObservableObject
    {
        private readonly TrophyDocument _doc = new TrophyDocument();
        private List<TrophyRow> _allRows = new List<TrophyRow>();
        private bool _dirty;

        /// <summary>True when there are edits not yet saved back to the trophy folder.</summary>
        public bool HasUnsavedChanges => _dirty;

        public ObservableCollection<TrophyRow> Trophies { get; } = new ObservableCollection<TrophyRow>();

        private string _gameTitle = "No game loaded";
        public string GameTitle { get => _gameTitle; set => Set(ref _gameTitle, value); }

        private string _gameSubtitle = "Open a trophy folder, or drag one here";
        public string GameSubtitle { get => _gameSubtitle; set => Set(ref _gameSubtitle, value); }

        private int _completionPercent;
        public int CompletionPercent { get => _completionPercent; set { Set(ref _completionPercent, value); Raise(nameof(CompletionText)); } }
        public string CompletionText => CompletionPercent + "%";

        private System.Windows.Media.ImageSource _gameIcon;
        public System.Windows.Media.ImageSource GameIcon { get => _gameIcon; set => Set(ref _gameIcon, value); }

        private bool _hasGame;
        public bool HasGame { get => _hasGame; set { Set(ref _hasGame, value); Raise(nameof(EmptyHintVisible)); } }
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
            OpenCommand = new RelayCommand(Open);
            SaveCommand = new RelayCommand(Save, () => _doc.IsOpen);
            RefreshCommand = new RelayCommand(Refresh, () => _doc.IsOpen);
            ScrapeCommand = new RelayCommand(async () => await ScrapeAsync(), () => _doc.IsOpen && !IsBusy);
            ClearCommand = new RelayCommand(ClearAll, () => _doc.IsOpen);
        }

        private static Window Owner => Application.Current.MainWindow;

        public void Open()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                OpenPath(dlg.SelectedPath);
            }
        }

        public void OpenPath(string folder)
        {
            try
            {
                _doc.Open(folder);
                _dirty = false;
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Owner, ex.Message, "Open failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        public void Save()
        {
            try
            {
                _doc.Save();
                _dirty = false;
                MessageBox.Show(Owner, "Saved.", "PS3TrophiesIsPerfect", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Owner, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearAll()
        {
            if (MessageBox.Show(Owner, "Lock every trophy (clear all unlock times)?", "Clear trophies",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _doc.ClearAll();
            _dirty = true;
            Refresh();
        }

        /// <summary>Double-click a row: set / change / (synced → blocked) the unlock time.</summary>
        public void EditRow(TrophyRow row)
        {
            if (row == null || !_doc.IsOpen) return;
            if (_doc.IsSynced(row.Id))
            {
                MessageBox.Show(Owner, "Trophy already synchronized. Can't be modified.", "Locked",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool got = _doc.IsGot(row.Id);
            DateTime initial = _doc.TimeOf(row.Id) ?? DateTime.Now;
            var dlg = new DateInputWindow(got ? "Change unlock time" : "Unlock time", initial) { Owner = Owner };
            if (dlg.ShowDialog() != true) return;

            try
            {
                if (got) _doc.ChangeTime(row.Id, dlg.SelectedDateTime);
                else _doc.Unlock(row.Id, dlg.SelectedDateTime);
                _dirty = true;
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Owner, ex.Message, "Can't apply", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task ScrapeAsync()
        {
            var urlDlg = new TextInputWindow { Owner = Owner };
            if (urlDlg.ShowDialog() != true) return;
            string url = urlDlg.Text;
            if (!PsnProfilesScraper.LooksLikeTrophyUrl(url))
            {
                MessageBox.Show(Owner, "Enter a PSNProfiles game-trophy URL, e.g.\n" +
                    "https://psnprofiles.com/trophies/41027-pragmata/SomeUser", "Copy from PSNProfiles",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show(Owner, ex.Message, "Scrape failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            IsBusy = false;

            if (scraped.Count == 0)
            {
                MessageBox.Show(Owner, "No earned trophies were found on that page.", "Copy from PSNProfiles",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            long[] times = _doc.MatchScrape(scraped, out int matched, out List<string> unmatched);
            if (unmatched.Count > 0)
            {
                const int max = 15;
                string list = string.Join("\n  • ", unmatched.Take(max));
                if (unmatched.Count > max) list += $"\n  … and {unmatched.Count - max} more";
                MessageBox.Show(Owner,
                    $"Matched {matched} of {matched + unmatched.Count} scraped trophies by name.\n\n" +
                    "These matched no trophy and were skipped:\n  • " + list,
                    "Copy from PSNProfiles", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (!times.Any(t => t != 0))
                return;

            // Offer the night-session relocation.
            if (MessageBox.Show(Owner,
                    "Rebuild this run as nightly play sessions from a start date through today, finishing " +
                    "with the platinum earned today?\n\nYes = pick the start date.   No = keep the scraped dates.",
                    "Relocate to night sessions", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dateDlg = new DateInputWindow("Start date — the first night of the run", DateTime.Today, showTime: false) { Owner = Owner };
                if (dateDlg.ShowDialog() == true)
                {
                    var r = _doc.RelocateToNightSessions(times, dateDlg.SelectedDateTime.Date);
                    MessageBox.Show(Owner,
                        $"Rebuilt across {r.Sessions} session(s) — " + (r.PlatEarned ? "platinum" : "last trophy") +
                        " earned just now.\n\nStarted:   " + r.First.ToString("yyyy/MM/dd  HH:mm:ss") +
                        "\nFinished:  " + r.Last.ToString("yyyy/MM/dd  HH:mm:ss"),
                        "Relocate to night sessions", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            try
            {
                _doc.ApplyTimes(times);
                _dirty = true;
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Owner, ex.Message, "Apply failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
