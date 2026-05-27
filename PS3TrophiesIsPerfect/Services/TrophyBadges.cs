using System.Collections.Generic;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Resolves the four trophy-type badge images (bronze / silver / gold / platinum) from the embedded
    /// <c>Assets/TrophyTypes/*.png</c>. Each badge is loaded once, frozen, and shared — so callers can read
    /// them freely from any thread and the same bitmap is reused everywhere.
    /// </summary>
    public static class TrophyBadges
    {
        private static readonly Dictionary<string, ImageSource> Cache =
            new Dictionary<string, ImageSource>();

        /// <summary>Badge for a TROPHYParser one-letter code: "P" / "G" / "S" / "B"; null for anything else.</summary>
        public static ImageSource ForCode(string code)
        {
            switch (code)
            {
                case "P":
                    return ForAsset("plat");
                case "G":
                    return ForAsset("gold");
                case "S":
                    return ForAsset("silver");
                case "B":
                    return ForAsset("bronze");
                default:
                    return null;
            }
        }

        /// <summary>Badge for a PSN type name: "platinum" / "gold" / "silver" / "bronze".</summary>
        public static ImageSource ForType(string type) =>
            ForAsset(type == "platinum" ? "plat" : type);

        /// <summary>Badge for a raw asset name ("plat" / "gold" / "silver" / "bronze"); null if unknown.</summary>
        public static ImageSource ForAsset(string asset)
        {
            if (string.IsNullOrEmpty(asset))
                return null;
            if (!Cache.TryGetValue(asset, out var img))
                Cache[asset] = img = ImageLoad.FromPack(
                    "pack://application:,,,/Assets/TrophyTypes/" + asset + ".png"
                );
            return img;
        }
    }
}
