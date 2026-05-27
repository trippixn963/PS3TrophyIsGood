using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Dialogs;
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

        // --- elevated drag-drop allowance (best effort) + maximize hook ---
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            const uint MSGFLT_ALLOW = 1;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            foreach (uint msg in new uint[] { 0x0233, 0x004A, 0x0049 })
                ChangeWindowMessageFilterEx(hwnd, msg, MSGFLT_ALLOW, IntPtr.Zero);

            // With a custom (WindowStyle=None) chrome, a maximized window would otherwise cover the taskbar
            // and clip its edges — clamp the maximized bounds to the monitor work area.
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(
            IntPtr hWnd,
            uint msg,
            uint action,
            IntPtr changeInfo
        );

        // --- custom title bar: window controls ---
        private void Min_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void MaxRestore_Click(object sender, RoutedEventArgs e) =>
            WindowState =
                WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            bool max = WindowState == WindowState.Maximized;
            MaxIcon.Glyph = ((char)(max ? 0xE923 : 0xE922)).ToString(); // restore / maximize
            MaxButton.ToolTip = max ? "Restore" : "Maximize";
        }

        // --- WM_GETMINMAXINFO: clamp maximize to the work area, keep the window's min size ---
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                IntPtr monitor = MonitorFromWindow(
                    hwnd,
                    0x2 /* NEAREST */
                );
                if (monitor != IntPtr.Zero)
                {
                    var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    GetMonitorInfo(monitor, ref info);
                    RECT work = info.rcWork,
                        mon = info.rcMonitor;
                    mmi.ptMaxPosition.x = work.left - mon.left;
                    mmi.ptMaxPosition.y = work.top - mon.top;
                    mmi.ptMaxSize.x = work.right - work.left;
                    mmi.ptMaxSize.y = work.bottom - work.top;
                    var dpi = VisualTreeHelper.GetDpi(this);
                    mmi.ptMinTrackSize.x = (int)(MinWidth * dpi.DpiScaleX);
                    mmi.ptMinTrackSize.y = (int)(MinHeight * dpi.DpiScaleY);
                    Marshal.StructureToPtr(mmi, lParam, true);
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTL
        {
            public int x,
                y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left,
                top,
                right,
                bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINTL ptReserved,
                ptMaxSize,
                ptMaxPosition,
                ptMinTrackSize,
                ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor,
                rcWork;
            public int dwFlags;
        }

        // --- window bounds persistence ---
        private void RestoreWindowBounds()
        {
            var s = Vm.Settings;
            if (s.WinWidth >= 300 && s.WinHeight >= 200)
            {
                Width = s.WinWidth;
                Height = s.WinHeight;
            }
            if (!double.IsNaN(s.WinLeft) && !double.IsNaN(s.WinTop))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = s.WinLeft;
                Top = s.WinTop;
            }
            if (s.WinMaximized)
                WindowState = WindowState.Maximized;
        }

        private void PersistWindowBounds()
        {
            var s = Vm.Settings;
            if (WindowState == WindowState.Maximized)
            {
                var rb = RestoreBounds;
                s.WinMaximized = true;
                s.WinLeft = rb.Left;
                s.WinTop = rb.Top;
                s.WinWidth = rb.Width;
                s.WinHeight = rb.Height;
            }
            else
            {
                s.WinMaximized = false;
                s.WinLeft = Left;
                s.WinTop = Top;
                s.WinWidth = Width;
                s.WinHeight = Height;
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
                    "You have unsaved changes. Save before closing?",
                    "Unsaved changes"
                );
                if (choice == Modern.SaveChoice.Cancel)
                    return;
                if (choice == Modern.SaveChoice.Save)
                    await Vm.SaveAsync(notify: false);
                _closeConfirmed = true;
                Close();
                return;
            }
            FlareSolverr.Stop();
            base.OnClosing(e);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (
                e.Key == Key.F
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            )
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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0]))
                await Vm.OpenPath(paths[0]);
        }
    }
}
