using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PS3TrophyIsGood
{
    /// <summary>
    /// Helpers for decrypting/encrypting PS3 trophy data via pfdtool and for
    /// copying trophy files in and out of a temporary working directory.
    /// </summary>
    public static class Utility
    {
        /// <summary>Signalled once the FlareSolverr proxy reports that it is serving.</summary>
        public static readonly ManualResetEvent servingReady = new ManualResetEvent(false);

        private const string PfdToolDirectory = "pfdtool";
        private const string PfdToolApp = PfdToolDirectory + "\\pfdtool.exe";
        private const string PfdToolBanner = "pfdtool 0.2.3 (c) 2012 by flatz\r\n\r\n";
        private const string DefaultProfile = "Default Profile";

        private static readonly string[] TrophyFileExtensions = { ".PFD", ".SFO", ".DAT", ".SFM" };

        /// <summary>
        /// Runs pfdtool with the given arguments and waits for it to exit.
        /// </summary>
        /// <param name="arguments">Command-line arguments passed to pfdtool.exe.</param>
        /// <returns>The tool's exit code and captured standard output.</returns>
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
                // Read stdout to completion before WaitForExit so a full pipe buffer can't deadlock us.
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
                throw new Exception(
                    "An error occurred while decrypting the trophies. Please make sure the "
                        + "Microsoft Visual C++ Redistributable is installed. You can download it at: "
                        + "https://www.microsoft.com/download/details.aspx?id=5555");
            }
            if (result.Output != PfdToolBanner)
            {
                throw new Exception(result.Output);
            }
        }

        public static void EncryptTrophy(string saveDir, string profile)
        {
            // When a non-default profile is selected, re-sign PARAM.SFO with that profile's id.
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
                using (var bw = new BinaryWriter(new FileStream(saveDir + "\\PARAM.SFO", FileMode.Open)))
                {
                    bw.BaseStream.Position = 0x274;
                    bw.Write(profileId);
                }
            }

            // Update the PFD, then encrypt the trophy data.
            RunPfdTool(" -u \"" + saveDir + "\"");
            RunPfdTool(" -e \"" + saveDir + "\" TROPTRNS.DAT");
        }

        public static long LongRandom(long min, long max, Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);
            return Math.Abs(longRand % (max - min)) + min;
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
                    "Source directory does not exist or could not be found: " + source);
            }

            // Create the destination directory if it doesn't exist.
            Directory.CreateDirectory(target);

            // Copy only the trophy-related files to the new location.
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
