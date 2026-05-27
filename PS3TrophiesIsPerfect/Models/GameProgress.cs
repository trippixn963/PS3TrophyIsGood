using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One PS3 game from the linked PSNProfiles account: banner + earned/total trophies + completion.</summary>
    public sealed class GameProgress
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string IconUrl { get; set; }

        /// <summary>The game banner (downloaded via the Cloudflare cookie + cached). Null if unavailable.</summary>
        public ImageSource Icon { get; set; }

        public int Earned { get; set; }
        public int Total { get; set; }
        public int Percent { get; set; }

        public string CountText => Earned + " / " + Total + " trophies";
        public string PercentText => Percent + "%";
    }
}
