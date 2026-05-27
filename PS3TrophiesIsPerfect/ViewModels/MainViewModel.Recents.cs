using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Infra;

namespace PS3TrophiesIsPerfect.ViewModels
{
    /// <summary>One recently opened trophy folder, shown on the welcome screen.</summary>
    public sealed class RecentFolder
    {
        public string Path { get; }
        public string Name { get; }
        public string Parent { get; }

        public RecentFolder(string path)
        {
            Path = path;
            string trimmed = path.TrimEnd('\\', '/');
            Name = System.IO.Path.GetFileName(trimmed);
            if (string.IsNullOrEmpty(Name))
                Name = trimmed;
            Parent = System.IO.Path.GetDirectoryName(trimmed) ?? "";
        }
    }

    // Recently opened folders, for one-click re-open on the welcome screen.
    public sealed partial class MainViewModel
    {
        public ObservableCollection<RecentFolder> RecentFolders { get; } =
            new ObservableCollection<RecentFolder>();

        public bool HasRecents => RecentFolders.Count > 0;

        private ICommand _openRecentCommand;
        public ICommand OpenRecentCommand =>
            _openRecentCommand
            ?? (
                _openRecentCommand = new RelayCommand<string>(path =>
                {
                    if (!string.IsNullOrEmpty(path))
                        _ = OpenPath(path);
                })
            );

        /// <summary>Pushes a folder to the front of the recents (deduped, capped at 8) and persists it.</summary>
        private void RememberFolder(string folder)
        {
            var list = Settings.RecentFolders ?? new List<string>();
            list.RemoveAll(p => string.Equals(p, folder, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, folder);
            if (list.Count > 8)
                list = list.Take(8).ToList();
            Settings.RecentFolders = list;
            Settings.Save();
            RebuildRecents();
        }

        /// <summary>Rebuilds the bound recents from settings, dropping folders that no longer exist.</summary>
        private void RebuildRecents()
        {
            RecentFolders.Clear();
            foreach (var p in Settings.RecentFolders ?? Enumerable.Empty<string>())
                if (Directory.Exists(p))
                    RecentFolders.Add(new RecentFolder(p));
            Raise(nameof(HasRecents));
        }
    }
}
