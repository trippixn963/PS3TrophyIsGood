using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.ViewModels;

namespace PS3TrophiesIsPerfect.Views
{
    public partial class LibraryView : UserControl
    {
        public LibraryView() => InitializeComponent();

        // Double-clicking a game card opens that game's trophy detail (ignored for the trophy rows themselves).
        private void Game_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (
                (e.OriginalSource as FrameworkElement)?.DataContext is GameProgress g
                && DataContext is MainViewModel vm
            )
                _ = vm.OpenGameAsync(g);
        }
    }
}
