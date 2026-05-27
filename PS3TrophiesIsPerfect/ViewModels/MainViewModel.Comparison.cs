using System.Collections.Generic;
using System.Collections.ObjectModel;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // The donor-vs-you comparison: rows, donor identity, and the gap verdict.
    public sealed partial class MainViewModel
    {
        public ObservableCollection<ComparisonRow> Comparison { get; } = new ObservableCollection<ComparisonRow>();
        private List<DonorEntry> _donor = new List<DonorEntry>();

        private bool _hasDonor;
        public bool HasDonor { get => _hasDonor; set => Set(ref _hasDonor, value); }

        private string _donorTitle = "";
        public string DonorTitle { get => _donorTitle; set => Set(ref _donorTitle, value); }

        private string _donorUser = "";
        public string DonorUser { get => _donorUser; set => Set(ref _donorUser, value); }

        private string _donorAvatarUrl = "";
        public string DonorAvatarUrl { get => _donorAvatarUrl; set => Set(ref _donorAvatarUrl, value); }

        private string _verdictText = "";
        public string VerdictText { get => _verdictText; set => Set(ref _verdictText, value); }

        private System.Windows.Media.Brush _verdictBrush = GreenBrush;
        public System.Windows.Media.Brush VerdictBrush { get => _verdictBrush; set => Set(ref _verdictBrush, value); }

        private static readonly System.Windows.Media.Brush GreenBrush = FrozenBrush(0x3F, 0xB9, 0x50);
        private static readonly System.Windows.Media.Brush RedBrush = FrozenBrush(0xF8, 0x51, 0x49);
        private static System.Windows.Media.Brush FrozenBrush(byte r, byte g, byte b)
        {
            var br = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            br.Freeze();
            return br;
        }

        private void ShowDonor(List<DonorEntry> entries, string title)
        {
            _donor = entries ?? new List<DonorEntry>();
            DonorTitle = string.IsNullOrEmpty(title) ? "Cloned from PSNProfiles" : title;
            HasDonor = _donor.Count > 0;
            RebuildComparison();
            if (HasDonor) SelectedTab = 1;
        }

        private void RebuildComparison()
        {
            Comparison.Clear();
            int exact = 0, slower = 0, faster = 0;
            if (HasDonor)
                foreach (var r in _doc.BuildComparison(_donor))
                {
                    Comparison.Add(r);
                    if (r.Match == "exact") exact++;
                    else if (r.Match == "slower") slower++;
                    else if (r.Match == "faster") faster++;
                }
            VerdictText = HasDonor ? $"{exact} exact · {slower} slower · {faster} faster" : "";
            VerdictBrush = faster > 0 ? RedBrush : GreenBrush;
        }

        private void ClearDonor()
        {
            _donor = new List<DonorEntry>();
            Comparison.Clear();
            HasDonor = false;
            DonorTitle = "";
            DonorUser = "";
            DonorAvatarUrl = "";
            SelectedTab = 0;
            Settings.Donor = new List<DonorEntry>();
            Settings.DonorTitle = "";
            Settings.DonorUser = "";
            Settings.DonorAvatarUrl = "";
            Settings.Save();
        }
    }
}
