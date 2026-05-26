using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.Services;
using PS3TrophiesIsPerfect.ViewModels;

namespace PS3TrophiesIsPerfect
{
    public partial class MainWindow : Window
    {
        private MainViewModel Vm => (MainViewModel)DataContext;
        private bool _closeConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowBounds();
            Loaded += async (s, e) => await Vm.StartupAsync();
        }

        // --- elevated drag-drop allowance (best effort) ---
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            const uint MSGFLT_ALLOW = 1;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            foreach (uint msg in new uint[] { 0x0233, 0x004A, 0x0049 })
                ChangeWindowMessageFilterEx(hwnd, msg, MSGFLT_ALLOW, IntPtr.Zero);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr changeInfo);

        // --- window bounds persistence ---
        private void RestoreWindowBounds()
        {
            var s = Vm.Settings;
            if (s.WinWidth >= 300 && s.WinHeight >= 200) { Width = s.WinWidth; Height = s.WinHeight; }
            if (!double.IsNaN(s.WinLeft) && !double.IsNaN(s.WinTop))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = s.WinLeft;
                Top = s.WinTop;
            }
            if (s.WinMaximized) WindowState = WindowState.Maximized;
        }

        private void PersistWindowBounds()
        {
            var s = Vm.Settings;
            if (WindowState == WindowState.Maximized)
            {
                var rb = RestoreBounds;
                s.WinMaximized = true;
                s.WinLeft = rb.Left; s.WinTop = rb.Top; s.WinWidth = rb.Width; s.WinHeight = rb.Height;
            }
            else
            {
                s.WinMaximized = false;
                s.WinLeft = Left; s.WinTop = Top; s.WinWidth = Width; s.WinHeight = Height;
            }
            s.Save();
        }

        // --- close: persist bounds + confirm unsaved changes ---
        protected override async void OnClosing(CancelEventArgs e)
        {
            PersistWindowBounds();
            if (!_closeConfirmed && Vm.HasUnsavedChanges)
            {
                e.Cancel = true;
                var choice = await Modern.SaveDiscardCancel(
                    "You have unsaved changes. Save before closing?", "Unsaved changes");
                if (choice == Modern.SaveChoice.Cancel) return;
                if (choice == Modern.SaveChoice.Save) await Vm.SaveAsync(notify: false);
                _closeConfirmed = true;
                Close();
                return;
            }
            FlareSolverr.Stop();
            base.OnClosing(e);
        }

        // --- list interactions ---
        private async void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row)
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
            if (Grid.SelectedItem is TrophyRow row)
                await Vm.EditRow(row);
        }

        private async void Ctx_Lock(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row)
                await Vm.LockRow(row);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                FilterBox.Focus();
                FilterBox.SelectAll();
                e.Handled = true;
            }
        }

        // --- folder drag-drop ---
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length > 0 && Directory.Exists(paths[0]))
                    e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0]))
                await Vm.OpenPath(paths[0]);
        }
    }
}
