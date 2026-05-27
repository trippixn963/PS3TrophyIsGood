using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Launches the bundled FlareSolverr proxy (needed for the Cloudflare-protected PSNProfiles scrape).
    /// No-ops gracefully if it's already running externally or the binary isn't present. Ported from the
    /// WinForms app's StartFlareSolverr.
    /// </summary>
    public static class FlareSolverr
    {
        private static Process _proc;

        public static void EnsureStarted()
        {
            if (IsReachable())
            {
                Utility.servingReady.Set(); // an instance is already serving
                return;
            }
            try
            {
                _proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "flaresolverr/flaresolverr.exe",
                        WorkingDirectory = "flaresolverr",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                _proc.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null && e.Data.Contains("Serving on"))
                        Utility.servingReady.Set();
                };
                _proc.Start();
                _proc.BeginOutputReadLine();
            }
            catch
            {
                // Not fatal: only the PSNProfiles scrape needs it.
                _proc = null;
            }
        }

        public static void Stop()
        {
            try
            {
                if (_proc != null && !_proc.HasExited)
                    _proc.Kill();
            }
            catch
            { /* already gone */
            }
        }

        private static bool IsReachable()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var ar = client.BeginConnect("127.0.0.1", 8191, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(500))
                        return false;
                    client.EndConnect(ar);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
