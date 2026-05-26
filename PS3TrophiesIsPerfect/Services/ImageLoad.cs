using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>Loads frozen, non-file-locking <see cref="ImageSource"/>s from disk or pack URIs.</summary>
    public static class ImageLoad
    {
        public static ImageSource FromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            return Load(new Uri(path, UriKind.Absolute));
        }

        public static ImageSource FromPack(string packUri) => Load(new Uri(packUri, UriKind.Absolute));

        private static ImageSource Load(Uri uri)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // read fully now so the file isn't locked
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = uri;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
