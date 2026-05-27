using System;
using System.ComponentModel;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One trophy in a game's detail view: icon, name, description, type, and this account's
    /// earned status + unlock time (from Sony's data).</summary>
    public sealed class TrophyDetail : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Detail { get; set; }
        public string IconUrl { get; set; }
        public string Type { get; set; } // bronze | silver | gold | platinum

        public bool Earned { get; set; }
        public DateTime? EarnedUtc { get; set; }

        public string TypeText => string.IsNullOrEmpty(Type) ? "" : char.ToUpper(Type[0]) + Type.Substring(1);

        public string EarnedText => Earned
            ? (EarnedUtc?.ToLocalTime().ToString("d MMM yyyy  h:mm tt") ?? "Earned")
            : "Locked";

        public double EarnedOpacity => Earned ? 1.0 : 0.45;

        /// <summary>The trophy's type badge — a local embedded asset, instant. (PSN says "platinum"; the asset is "plat".)</summary>
        public ImageSource TypeBadge =>
            ImageLoad.FromPack("pack://application:,,,/Assets/TrophyTypes/" + (Type == "platinum" ? "plat" : Type ?? "bronze") + ".png");

        /// <summary>The trophy graphic — downloaded from Sony's open CDN and cached, set after the list shows.</summary>
        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
