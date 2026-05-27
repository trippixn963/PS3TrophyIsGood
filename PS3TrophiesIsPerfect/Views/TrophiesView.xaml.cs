using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.ViewModels;

namespace PS3TrophiesIsPerfect.Views
{
    public partial class TrophiesView : UserControl
    {
        public TrophiesView() => InitializeComponent();

        private MainViewModel Vm => DataContext as MainViewModel;

        private async void Trophy_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is TrophyRow row && Vm != null)
                await Vm.EditRow(row);
        }

        private async void Ctx_Edit(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TrophyRow row && Vm != null)
                await Vm.EditRow(row);
        }

        private async void Ctx_Lock(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TrophyRow row && Vm != null)
                await Vm.LockRow(row);
        }
    }
}
