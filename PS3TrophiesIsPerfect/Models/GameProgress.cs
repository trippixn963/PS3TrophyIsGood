using System.ComponentModel;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One PS3 game from the linked PSNProfiles account: banner + earned/total trophies + completion.</summary>
    public sealed class GameProgress : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string GameId { get; set; }
        public string IconUrl { get; set; }

        public int Earned { get; set; }
        public int Total { get; set; }
        public int Percent { get; set; }

        public string CountText => Earned + " / " + Total + " trophies";
        public string PercentText => Percent + "%";

        /// <summary>The game banner — set after the list shows (downloaded in the background + cached).</summary>
        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
