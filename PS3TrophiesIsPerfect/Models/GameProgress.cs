namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One PS3 game from the linked PSNProfiles account: earned/total trophies + completion.</summary>
    public sealed class GameProgress
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string IconUrl { get; set; }
        public int Earned { get; set; }
        public int Total { get; set; }
        public int Percent { get; set; }

        public string CountText => Earned + " / " + Total + " trophies";
        public string PercentText => Percent + "%";
    }
}
