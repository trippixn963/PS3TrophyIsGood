using System;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // The bottom status bar: at-a-glance folder / save / account / scrape state.
    public sealed partial class MainViewModel
    {
        private DateTime? _lastSaved;

        public string StatusFolder => _doc.IsOpen ? _doc.GamePath : "No folder open";

        public string StatusSave =>
            HasUnsavedChanges ? "● Unsaved changes"
            : _lastSaved.HasValue ? "Saved " + _lastSaved.Value.ToString("HH:mm")
            : _doc.IsOpen ? "No changes"
            : "";

        public string StatusAccount => HasMyUser ? "PSN · " + MyPsnUser : "PSN · not linked";

        public string ScrapeStatus =>
            Utility.servingReady.WaitOne(0) ? "Scrape ready" : "Scrape · starting…";

        /// <summary>Refreshes every status-bar field at once.</summary>
        private void RaiseStatus()
        {
            Raise(nameof(StatusFolder));
            Raise(nameof(StatusSave));
            Raise(nameof(StatusAccount));
            Raise(nameof(ScrapeStatus));
        }
    }
}
