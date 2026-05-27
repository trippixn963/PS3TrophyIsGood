using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Models;

namespace PS3TrophiesIsPerfect.Views
{
    public partial class LibraryView : UserControl
    {
        public LibraryView() => InitializeComponent();

        private void Games_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is GameProgress g && !string.IsNullOrEmpty(g.Url))
            {
                try { System.Diagnostics.Process.Start(g.Url); }
                catch { /* no browser / blocked */ }
            }
        }
    }
}
