using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Models;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // The trophy grid: the projected rows, the name filter, and per-trophy edits.
    public sealed partial class MainViewModel
    {
        private List<TrophyRow> _allRows = new List<TrophyRow>();

        public ObservableCollection<TrophyRow> Trophies { get; } =
            new ObservableCollection<TrophyRow>();

        private string _filter = "";
        public string Filter
        {
            get => _filter;
            set
            {
                Set(ref _filter, value);
                ApplyFilter();
            }
        }

        public string[] TrophySorts { get; } = { "Unlock order", "Name", "Type", "Most recent" };
        private string _trophySort = "Unlock order";
        public string TrophySort
        {
            get => _trophySort;
            set
            {
                Set(ref _trophySort, value);
                ApplyFilter();
            }
        }

        private string _trophyCountText = "";
        public string TrophyCountText
        {
            get => _trophyCountText;
            private set => Set(ref _trophyCountText, value);
        }

        private static int TypeRank(string t)
        {
            switch (t)
            {
                case "P":
                    return 0;
                case "G":
                    return 1;
                case "S":
                    return 2;
                case "B":
                    return 3;
                default:
                    return 4;
            }
        }

        private void ApplyFilter()
        {
            Trophies.Clear();
            IEnumerable<TrophyRow> rows = _allRows;
            if (!string.IsNullOrWhiteSpace(_filter))
                rows = rows.Where(r =>
                    (r.Name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0
                );

            switch (_trophySort)
            {
                case "Name":
                    rows = rows.OrderBy(r => r.Name);
                    break;
                case "Type":
                    rows = rows.OrderBy(r => TypeRank(r.Type)).ThenBy(r => r.Order);
                    break;
                case "Most recent":
                    rows = rows.OrderByDescending(r => r.Time ?? DateTime.MinValue)
                        .ThenBy(r => r.Order);
                    break;
                default:
                    rows = rows.OrderBy(r => r.Order);
                    break;
            }

            int shown = 0;
            foreach (var r in rows)
            {
                Trophies.Add(r);
                shown++;
            }
            TrophyCountText =
                _allRows.Count == shown
                    ? shown + " trophies"
                    : shown + " of " + _allRows.Count + " trophies";
        }

        /// <summary>Double-click / context menu: set / change the unlock time.</summary>
        public async Task EditRow(TrophyRow row)
        {
            if (row == null || !_doc.IsOpen)
                return;
            if (_doc.IsSynced(row.Id))
            {
                await Modern.Info("Trophy already synchronized. Can't be modified.", "Locked");
                return;
            }
            bool got = _doc.IsGot(row.Id);
            DateTime initial = _doc.TimeOf(row.Id) ?? DateTime.Now;
            DateTime? picked = await Modern.PromptDate(
                got ? "Change unlock time" : "Unlock time",
                initial,
                showTime: true
            );
            if (picked == null)
                return;
            try
            {
                if (got)
                    _doc.ChangeTime(row.Id, picked.Value);
                else
                    _doc.Unlock(row.Id, picked.Value);
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
            if (row == null || !_doc.IsOpen)
                return;
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
    }
}
