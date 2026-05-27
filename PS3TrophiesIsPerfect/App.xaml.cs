using System;
using System.IO;
using System.Windows;

namespace PS3TrophiesIsPerfect
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // pfdtool\, flaresolverr\ and profiles\ are resolved relative to the working directory.
            // Pin it to the exe's own folder so the app works no matter how it was launched (Explorer
            // hand-off, a shortcut, a parent process, etc.) instead of only when started from the folder.
            try { Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); }
            catch { /* best effort */ }
            base.OnStartup(e);
        }
    }
}
