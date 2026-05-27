using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Helpers for decrypting/encrypting PS3 trophy data via pfdtool and for copying trophy files in
    /// and out of a temporary working directory. Ported verbatim from the WinForms app — this is
    /// app-layer plumbing, not part of the frozen TROPHYParser core.
    /// </summary>
    public static class Utility
    {
        /// <summary>Signalled once the FlareSolverr proxy is known to be serving.</summary>
        public static readonly ManualResetEvent servingReady = new ManualResetEvent(false);

        private const string PfdToolDirectory = "pfdtool";
        private const string PfdToolApp = PfdToolDirectory + "\\pfdtool.exe";
        private const string PfdToolBanner = "pfdtool 0.2.3 (c) 2012 by flatz\r\n\r\n";
        private const string DefaultProfile = "Default Profile";

        private static readonly string[] TrophyFileExtensions = { ".PFD", ".SFO", ".DAT", ".SFM" };

        private static (int ExitCode, string Output) RunPfdTool(string arguments)
        {
            var startInfo = new ProcessStartInfo(PfdToolApp, arguments)
            {
                WorkingDirectory = PfdToolDirectory,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var proc = new Process { StartInfo = startInfo })
            {
                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return (proc.ExitCode, output);
            }
        }

        public static void DecryptTrophy(string saveDir)
        {
            var result = RunPfdTool(" -d \"" + saveDir + "\" TROPTRNS.DAT");
            if (result.ExitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(result.Output)
                    ? "(no output)"
                    : result.Output.Trim();
                throw new Exception(
                    "Couldn't decrypt the trophy data (pfdtool exited "
                        + result.ExitCode
                        + ").\n\n"
                        + detail
                        + "\n\nIf pfdtool didn't run at all, install the Microsoft Visual C++ Redistributable: "
                        + "https://www.microsoft.com/download/details.aspx?id=5555"
                );
            }
            if (result.Output != PfdToolBanner)
            {
                throw new Exception(result.Output);
            }
        }

        public static void EncryptTrophy(string saveDir, string profile)
        {
            if (profile != DefaultProfile)
            {
                profile = "profiles\\" + profile;
                byte[] profileId;
                using (var br = new BinaryReader(new FileStream(profile, FileMode.Open)))
                {
                    br.BaseStream.Position = 0xC;
                    br.BaseStream.Position = br.ReadInt32();
                    profileId = br.ReadBytes(0x10);
                }
                using (
                    var bw = new BinaryWriter(
                        new FileStream(saveDir + "\\PARAM.SFO", FileMode.Open)
                    )
                )
                {
                    bw.BaseStream.Position = 0x274;
                    bw.Write(profileId);
                }
            }

            RunPfdTool(" -u \"" + saveDir + "\"");
            RunPfdTool(" -e \"" + saveDir + "\" TROPTRNS.DAT");
        }

        public static DateTime TimeStampToDateTime(this long timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory;
            do
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(tempDirectory));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static string CopyTrophyDirToTemp(string trophyDir)
        {
            DirectoryInfo dir = new DirectoryInfo(trophyDir);
            string pathTemp = Path.Combine(GetTemporaryDirectory(), dir.Name);
            CopyTrophyData(trophyDir, pathTemp, true);
            return pathTemp;
        }

        public static void CopyTrophyData(string source, string target, bool overwrite)
        {
            DirectoryInfo dir = new DirectoryInfo(source);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: " + source
                );
            }

            Directory.CreateDirectory(target);

            foreach (FileInfo file in dir.GetFiles())
            {
                if (TrophyFileExtensions.Contains(file.Extension.ToUpper()))
                {
                    file.CopyTo(Path.Combine(target, file.Name), overwrite);
                }
            }
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
