using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PS3TrophiesIsPerfect.Models;
using PS3TrophiesIsPerfect.ViewModels;

namespace PS3TrophiesIsPerfect
{
    public partial class MainWindow : Window
    {
        private MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => AllowDragDropAcrossElevation();
        }

        /// <summary>
        /// When the app runs elevated, Windows' UIPI blocks drag-drop from a normal-integrity Explorer.
        /// Explicitly allow the drag-drop window messages so dropping a folder works even as admin.
        /// </summary>
        private void AllowDragDropAcrossElevation()
        {
            const uint MSGFLT_ALLOW = 1;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            foreach (uint msg in new uint[] { 0x0233 /*WM_DROPFILES*/, 0x004A /*WM_COPYDATA*/, 0x0049 /*WM_COPYGLOBALDATA*/ })
                ChangeWindowMessageFilterEx(hwnd, msg, MSGFLT_ALLOW, IntPtr.Zero);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr changeInfo);

        private void Grid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Grid.SelectedItem is TrophyRow row)
                Vm.EditRow(row);
        }

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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0]))
                Vm.OpenPath(paths[0]);
        }
    }
}
