using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.ViewModels;

namespace PS3TrophiesIsPerfect.Views
{
    public partial class TrophiesView : UserControl
    {
        public TrophiesView() => InitializeComponent();

        private MainViewModel Vm => DataContext as MainViewModel;

        private async void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row && Vm != null)
                await Vm.EditRow(row);
        }

        private void Grid_RightDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = e.OriginalSource as DependencyObject;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);
            if (dep is DataGridRow row)
                row.IsSelected = true;
        }

        private async void Ctx_Edit(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row && Vm != null)
                await Vm.EditRow(row);
        }

        private async void Ctx_Lock(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row && Vm != null)
                await Vm.LockRow(row);
        }
    }
}
