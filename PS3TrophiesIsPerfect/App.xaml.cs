using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Make ModernWpf's own accent (tab indicator, progress ring, combo/focus highlights) the logo blue.
            try { ModernWpf.ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString("#0188F5"); }
            catch { /* fall back to the default accent */ }

            // pfdtool\, flaresolverr\ and profiles\ are resolved relative to the working directory.
            // Pin it to the exe's own folder so the app works no matter how it was launched (Explorer
            // hand-off, a shortcut, a parent process, etc.) instead of only when started from the folder.
            try { Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); }
            catch { /* best effort */ }

            // Sony's auth + trophy endpoints (and their image CDN) require TLS 1.2+. .NET 4.8 usually
            // negotiates this, but pin it so HTTPS to PSN never silently fails on a stale default.
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)12288; // Tls13, if the OS supports it
            }
            catch { /* older OS without TLS 1.3 — TLS 1.2 alone is fine */ }

            base.OnStartup(e);
        }
    }
}
