using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TROPHYParser;

namespace PS3TrophyIsGood
{
    public partial class MainAPP : Form
    {
        private const long MINIMUM_POSSIBLE_DATE = 633347424000000000;
        private Process process;
        TROPCONF tconf;
        TROPTRNS tpsn;
        TROPUSR tusr;
        string path;
        string pathTemp;
        DateTimePickForm dtpForm = null;
        DateTimePickForm dtpfForInstant = null;
        CopyFrom copyFrom = null;
        bool haveBeenEdited = false;

        DateTime ps3Time = new DateTime(MINIMUM_POSSIBLE_DATE);
        DateTime lastSyncTrophyTime = new DateTime(MINIMUM_POSSIBLE_DATE);
        DateTime randomEndTime = DateTime.Now;

        bool isOpen = false;
        int baseGameCount;

        private string txtDateTimeTmp;

        public MainAPP()
        {
            CultureInfo curinfo = null;
            switch (Properties.Settings.Default.Language)
            {
                case 0:
                    curinfo = new CultureInfo("zh-TW");
                    break;
                case 2:
                    curinfo = new CultureInfo("pt-BR");
                    break;
                case 1:
                default:
                    curinfo = CultureInfo.CreateSpecificCulture("en");
                    break;
            }

            Thread.CurrentThread.CurrentCulture = curinfo;
            Thread.CurrentThread.CurrentUICulture = curinfo;
            InitializeComponent();
            BuildColorLegend();
            toolStripComboBox1.SelectedIndexChanged -= toolStripComboBox1_SelectedIndexChanged;
            toolStripComboBox1.SelectedIndex = Properties.Settings.Default.Language;
            toolStripComboBox1.SelectedIndexChanged += toolStripComboBox1_SelectedIndexChanged;
            Directory.CreateDirectory("profiles");
            var profiles = new DirectoryInfo("profiles").GetFiles("*.sfo").Select(p => p.Name).ToArray();
            toolStripComboBox2.Items.Add("Default Profile");
            toolStripComboBox2.Items.AddRange(profiles);
            toolStripComboBox2.SelectedIndex = 0;
            dateTimePicker1.CustomFormat = Properties.strings.DateFormatString;
            copyFrom = new CopyFrom();
            StartFlareSolverr();
        }

        /// <summary>
        /// Launches the FlareSolverr proxy used by the URL-scrape import path. FlareSolverr is
        /// optional: if it isn't present (e.g. the JSON-import path or normal editing is all the
        /// user needs), startup continues without it instead of crashing.
        /// </summary>
        private void StartFlareSolverr()
        {
            try
            {
                process = new Process
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
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                        if (e.Data.Contains("Serving on"))
                        {
                            Utility.servingReady.Set(); // signal that FlareSolverr is ready
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                // Not fatal: only the URL-scrape import needs FlareSolverr.
                process = null;
                Console.WriteLine("FlareSolverr did not start; URL import will be unavailable. " + ex.Message);
            }
        }

        /// <summary>Stops the FlareSolverr proxy if it is running. Safe to call when it never started.</summary>
        private void StopFlareSolverr()
        {
            try
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch { /* already gone */ }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/trippixn963/PS3TrophyIsGood");
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
            StopFlareSolverr();
            Application.Exit();
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenFile(folderBrowserDialog1.SelectedPath);
            }
        }

        private bool ValidateSelectedDate(DateTime selectedDate)
        {
            if (DateTime.Compare(lastSyncTrophyTime, selectedDate) > 0)
            {
                MessageBox.Show(string.Format(Properties.strings.PsnSyncTime, lastSyncTrophyTime.ToString(Properties.strings.DateFormatString)));
                return false;
            }
            return true;
        }

        private void DeleteTrophy(int trophyId, ListViewItem lvi)
        {
            if (IsTrophySync(trophyId))
            {
                MessageBox.Show(Properties.strings.SyncedTrophyCanNotEdit);
            }
            else
            if (trophyId != 0 && tconf[trophyId].gid == 0 && IsTrophyGot(0))
            {
                MessageBox.Show(Properties.strings.CantLoclPlatinumBeforOther);
            }
            else
            if (MessageBox.Show(Properties.strings.DeleteTrophyConfirm, Properties.strings.Delete, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                tpsn.DeleteTrophyByID(trophyId);
                tusr.LockTrophy(trophyId);
                lvi.SubItems[4].Text = Properties.strings.no;
                lvi.BackColor = Color.LightGray;
                lvi.SubItems[6].Text = string.Empty;
                CompletionRates();
                RefreshTimeDiffColumn();
                haveBeenEdited = true;
            }
        }

        private bool UnlockTrophy(int trophyId, DateTime trophyTime, ListViewItem lvi)
        {
            if (trophyId == 0 && tconf.HasPlatinium && (GetCountBaseTrophiesGot() < baseGameCount))
            {
                MessageBox.Show(Properties.strings.CantUnloclPlatinumBeforOther);
                return false;
            }
            else
            {
                if (ValidateSelectedDate(trophyTime))
                {
                    try
                    {
                        tpsn.PutTrophy(trophyId, tusr.trophyTypeTable[trophyId].Type, trophyTime);
                        tusr.UnlockTrophy(trophyId, trophyTime);
                        lvi.SubItems[4].Text = Properties.strings.yes;
                        lvi.BackColor = Color.White;
                        lvi.SubItems[6].Text = trophyTime.ToString(Properties.strings.DateFormatString);
                        CompletionRates();
                        RefreshTimeDiffColumn();
                        haveBeenEdited = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private bool ChangeTrophyTime(int trophyId, DateTime trophyTime, ListViewItem lvi)
        {
            if (IsTrophySync(trophyId))
            {
                MessageBox.Show(Properties.strings.SyncedTrophyCanNotEdit);
                return false;
            }
            else
            {
                if (ValidateSelectedDate(trophyTime))
                {
                    try
                    {
                        tpsn.ChangeTime(trophyId, trophyTime);
                        TROPUSR.TrophyTimeInfo tti = tusr.trophyTimeInfoTable[trophyId];
                        tti.Time = trophyTime;
                        tusr.trophyTimeInfoTable[trophyId] = tti;
                        lvi.SubItems[6].Text = trophyTime.ToString(Properties.strings.DateFormatString);
                        RefreshTimeDiffColumn();
                        haveBeenEdited = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private void RefreshComponents()
        {
            EmptyAllComponents();
            var timeDiffs = ComputeTimeDiffStrings();
            listViewEx1.BeginUpdate();
            for (int i = 0; i < tconf.Count; i++)
            {
                listViewEx1.LargeImageList.Images.Add("", Image.FromFile(path + @"\TROP" + string.Format("{0:000}", tconf[i].id) + ".PNG"));
                ListViewItem lvi = new ListViewItem();
                lvi.ImageIndex = i; // Note: the ListView ImageIndex doubles as the trophy ID (e.g. Platinum = 0, 1...)
                lvi.Text = tconf[i].name;
                lvi.SubItems.Add(tconf[i].detail);
                lvi.SubItems.Add(tconf[i].ttype);
                lvi.SubItems.Add(tconf[i].hidden == "yes" ? Properties.strings.yes : Properties.strings.no);
                if (tpsn[i].HasValue)
                {
                    lvi.SubItems.Add(Properties.strings.yes);
                    lvi.SubItems.Add(tpsn[i].Value.IsSync ? Properties.strings.yes : Properties.strings.no);
                    lvi.SubItems.Add(tpsn[i].Value.Time.ToString(Properties.strings.DateFormatString));
                    lvi.BackColor = (tpsn[i].Value.IsSync ? Color.LightPink : lvi.BackColor = Color.White);
                }
                else
                {
                    lvi.SubItems.Add(tusr.trophyTimeInfoTable[i].IsGet ? Properties.strings.yes : Properties.strings.no);
                    lvi.SubItems.Add(tusr.trophyTimeInfoTable[i].IsSync ? Properties.strings.yes : Properties.strings.no);
                    
                    var tropTimeTxt = string.Empty;
                    if (tusr.trophyTimeInfoTable[i].Time.Ticks > 0)
                    {
                        tropTimeTxt = tusr.trophyTimeInfoTable[i].Time.ToString(Properties.strings.DateFormatString);
                    }
                    lvi.SubItems.Add(tropTimeTxt);

                    if (tusr.trophyTimeInfoTable[i].IsSync)
                        lvi.BackColor = Color.LightPink;
                    else if (tusr.trophyTimeInfoTable[i].IsGet)
                        lvi.BackColor = Color.White;
                    else
                        lvi.BackColor = Color.LightGray;
                }
                if (tconf[i].gid == 0)
                {
                    lvi.SubItems.Add("Base Game");
                    baseGameCount = i;
                }
                else lvi.SubItems.Add($"DLC{tconf[i].gid}");

                lvi.SubItems.Add(timeDiffs.TryGetValue(i, out string diff) ? diff : string.Empty);

                listViewEx1.Items.Add(lvi);
            }
            listViewEx1.EndUpdate();
            CompletionRates();
        }

        private void EmptyAllComponents()
        {
            listViewEx1.Items.Clear();
            listViewEx1.LargeImageList.Images.Clear();
            listViewEx1.LargeImageList.ImageSize = new Size(50, 50);
            this.Text = Application.ProductName;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            label2.Text = "00/00";
            label4.Text = "000/000";
        }

        private void CompletionRates()
        {
            int totalGrade = 0, getGrade = 0, isGetTrophyNumber = 0;
            for (int i = 0; i < tconf.Count; i++)
            {
                switch ((TropType)tusr.trophyTypeTable[i].Type)
                {
                    case TropType.Platinum:
                        totalGrade += (int)TropGrade.Platinum;
                        getGrade += IsTrophySync(i) ? (int)TropGrade.Platinum : 0;
                        break;
                    case TropType.Gold:
                        totalGrade += (int)TropGrade.Gold;
                        getGrade += IsTrophySync(i) ? (int)TropGrade.Gold : 0;
                        break;
                    case TropType.Silver:
                        totalGrade += (int)TropGrade.Silver;
                        getGrade += IsTrophySync(i) ? (int)TropGrade.Silver : 0;
                        break;
                    case TropType.Bronze:
                        totalGrade += (int)TropGrade.Bronze;
                        getGrade += IsTrophySync(i) ? (int)TropGrade.Bronze : 0;
                        break;
                }

                if (IsTrophySync(i)) isGetTrophyNumber++;
            }
            progressBar1.Maximum = totalGrade;
            progressBar1.Value = getGrade;
            label2.Text = isGetTrophyNumber + "/" + tconf.Count;
            label4.Text = getGrade + "/" + totalGrade;
            this.Text = Application.ProductName + "-[" + tconf.title_name + "]";
        }

        private bool IsTrophySync(int trophyID)
        {
            return (tpsn[trophyID].HasValue && tpsn[trophyID].Value.IsSync) || tusr.trophyTimeInfoTable[trophyID].IsSync;
        }

        private bool IsTrophyGot(int trophyID)
        {
            return (!isRpcs3Format.Checked && tpsn[trophyID].HasValue) || tusr.trophyTimeInfoTable[trophyID].IsGet;
        }

        private int GetCountBaseTrophiesGot()
        {
            int countBaseTrophiesGot = 0;
            for (int i = 0; i < tconf.trophys.Count; i++)
            {
                if (tconf[i].gid == 0 && IsTrophyGot(i))
                {
                    countBaseTrophiesGot++;
                }
            }
            return countBaseTrophiesGot;
        }

        private void listViewEx1_SubItemClicked(object sender, ListViewEx.SubItemEventArgs e)
        {
            int trophyID = e.Item.ImageIndex;// Note: the ListView ImageIndex doubles as the trophy ID (e.g. Platinum = 0, 1...)
            if (e.SubItem == 6 && !IsTrophySync(trophyID))
            {
                DateTime trophyTime = DateTime.Now;
                if (IsTrophyGot(trophyID))
                {
                    if (tpsn[trophyID].HasValue)
                    {
                        trophyTime = tpsn[trophyID].Value.Time;
                    }
                    else
                    {
                        trophyTime = tusr.trophyTimeInfoTable[trophyID].Time;
                    }
                }
                txtDateTimeTmp = e.Item.SubItems[e.SubItem].Text;
                e.Item.SubItems[e.SubItem].Text = trophyTime.ToString(Properties.strings.DateFormatString);
                listViewEx1.StartEditing(dateTimePicker1, e.Item, e.SubItem);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
                if (Directory.Exists(files[0]))
                {
                    e.Effect = DragDropEffects.All;
                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            OpenFile(files[0]);
        }

        private void refreshMenuItem_Click(object sender, EventArgs e)
        {
            RefreshComponents();
        }

        private void listViewEx1_SubItemEndEditing(object sender, ListViewEx.SubItemEndEditingEventArgs e)
        {
            if (isOpen)
            {
                DateTime selectedDate = Convert.ToDateTime(e.DisplayText);
                var trophyID = e.Item.ImageIndex;
                ListViewItem lvi = ((ListView)sender).SelectedItems[0];
                bool trophyChanged;
                if (tpsn[trophyID].HasValue)
                {
                    trophyChanged = ChangeTrophyTime(trophyID, selectedDate, lvi);
                }
                else
                {
                    trophyChanged = UnlockTrophy(trophyID, selectedDate, lvi);
                }

                if (!trophyChanged)
                {
                    e.DisplayText = txtDateTimeTmp;
                }
            }
        }

        private void listViewEx1_DoubleClick(object sender, EventArgs e)
        {
            int trophyID = ((ListView)sender).SelectedItems[0].ImageIndex;// Note: the ListView ImageIndex doubles as the trophy ID (e.g. Platinum = 0, 1...)
            ListViewItem lvi = ((ListView)sender).SelectedItems[0];
            if (IsTrophySync(trophyID))
            { // only un-synced trophies can be edited
                MessageBox.Show(Properties.strings.SyncedTrophyCanNotEdit);
            }
            else if (tpsn[trophyID].HasValue || (isRpcs3Format.Checked && IsTrophyGot(trophyID)))
            {
                DeleteTrophy(trophyID, lvi);
            }
            else
            {  // nonget
                if (trophyID == 0 && tconf.HasPlatinium && (GetCountBaseTrophiesGot() < baseGameCount))
                {
                    MessageBox.Show(Properties.strings.CantUnloclPlatinumBeforOther);
                }
                else if (dtpForm.ShowDialog(this) == DialogResult.OK)
                {
                    UnlockTrophy(trophyID, dtpForm.dateTimePicker1.Value, lvi);
                }
            }
        }

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void closeFileMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private DateTime LastTrophyTime()
        {
            if (DateTime.Compare(tpsn.LastTrophyTime, tusr.LastTrophyTime) > 0)
            {
                return tpsn.LastTrophyTime;
            }
            return tusr.LastTrophyTime;
        }

        private void OpenFile(string path_in)
        {
            try
            {
                if (isOpen)
                {
                    CloseFile();
                }
                path = path_in;
                pathTemp = Utility.CopyTrophyDirToTemp(path_in);
                Utility.DecryptTrophy(pathTemp);
                tconf = new TROPCONF(pathTemp, isRpcs3Format.Checked);
                tpsn = new TROPTRNS(pathTemp, isRpcs3Format.Checked);
                tusr = new TROPUSR(pathTemp, isRpcs3Format.Checked);

                lastSyncTrophyTime = tusr.LastSyncTime;
                if (DateTime.Compare(tpsn.LastSyncTime, tusr.LastSyncTime) > 0)
                    lastSyncTrophyTime = tpsn.LastSyncTime;

                ps3Time = lastSyncTrophyTime;
                dtpForm = new DateTimePickForm(ps3Time);
                dtpfForInstant = new DateTimePickForm(ps3Time);

                RefreshComponents();
                isOpen = true;
                refreshMenuItem.Enabled = true;
                advancedMenuItem.Enabled = true;
            }
            catch (FileNotFoundException ex)
            {
                tconf = null;
                tpsn = null;
                tusr = null;
                GC.Collect();
                MessageBox.Show(string.Format(Properties.strings.FileNotFoundMsg, Path.GetFileName(ex.FileName)));
            }
            catch (Exception ex)
            {
                tconf = null;
                tpsn = null;
                tusr = null;
                GC.Collect();
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveFile()
        {
            if (isOpen)
            {
                if (listViewEx1.IsEditing)
                    listViewEx1.EndEditing(true);
                tpsn.Save();
                tusr.Save();
                haveBeenEdited = false;
                string encPathTemp = Utility.GetTemporaryDirectory();
                try
                {
                    Utility.CopyTrophyData(pathTemp, encPathTemp, false);
                    Utility.EncryptTrophy(encPathTemp, toolStripComboBox2.Text);
                    Utility.CopyTrophyData(encPathTemp, path, true);
                }
                finally
                {
                    Utility.DeleteDirectory(encPathTemp);
                }
                RefreshComponents();
            }
        }

        public bool CloseFile()
        {
            if (listViewEx1.IsEditing)
                listViewEx1.EndEditing(true);
            if (haveBeenEdited)
            {
                DialogResult dr = MessageBox.Show(Properties.strings.CloseConfirm, Properties.strings.Close, MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes)
                {
                    SaveFile();
                }
                else if (dr == DialogResult.Cancel)
                {
                    return false; // abort closing; "No" falls through and closes without saving
                }
            }

            tpsn = null;
            tusr = null;
            tconf = null;
            path = string.Empty;
            pathTemp = string.Empty;
            haveBeenEdited = false;
            refreshMenuItem.Enabled = false;
            advancedMenuItem.Enabled = false;
            isOpen = false;
            EmptyAllComponents();
            if (!string.IsNullOrEmpty(pathTemp))
            {
                Utility.DeleteDirectory(new DirectoryInfo(pathTemp).Parent.FullName);
            }
            return true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isOpen)
            {
                e.Cancel = !CloseFile();
            }

            StopFlareSolverr();
        }

        private void instantPlatinumMenuItem_Click(object sender, EventArgs e)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            int i;

            //Base game
            for (i = 1; i < tusr.trophyTimeInfoTable.Count && tconf[i].gid == 0; i++)
            {
                if (!IsTrophyGot(i))
                {
                    tusr.UnlockTrophy(i, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                    tpsn.PutTrophy(i, tusr.trophyTypeTable[i].Type, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                }
            }
            //Platinium game
            if (!IsTrophyGot(0))
            {
                tusr.UnlockTrophy(0, LastTrophyTime().AddSeconds(1));
                tpsn.PutTrophy(0, tusr.trophyTypeTable[0].Type, LastTrophyTime().AddSeconds(1));
            }

            //DLC 
            for (; i < tusr.trophyTimeInfoTable.Count; i++)
            {
                if (!IsTrophyGot(i))
                {
                    tusr.UnlockTrophy(i, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                    tpsn.PutTrophy(i, tusr.trophyTypeTable[i].Type, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                }
            }
            haveBeenEdited = true;
            RefreshComponents();
        }

        private void clearTrophiesMenuItem_Click(object sender, EventArgs e)
        {
            TROPTRNS.TrophyInfo? ti = tpsn.PopTrophy();
            while (ti.HasValue)
            {
                tusr.LockTrophy(ti.Value.TrophyID);
                ti = tpsn.PopTrophy();
            }
            haveBeenEdited = true;
            RefreshComponents();
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Language = toolStripComboBox1.SelectedIndex;
            Properties.Settings.Default.Save();
            MessageBox.Show(Properties.strings.RestartProgram);
        }

        private void setRandomStartTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dtpfForInstant.Title.Text = Properties.strings.RandomStartTime;
            if (dtpfForInstant.ShowDialog() == DialogResult.OK)
            {
                ps3Time = dtpfForInstant.dateTimePicker1.Value;
            }
        }

        private void setRandomEndTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dtpfForInstant.Title.Text = Properties.strings.RandomEndTime;
            if (dtpfForInstant.ShowDialog() == DialogResult.OK)
            {
                randomEndTime = dtpfForInstant.dateTimePicker1.Value;
            }
        }

        /// <summary>
        /// Normalizes a trophy name for tolerant matching: applies Unicode NFKC (folds full-width and
        /// other compatibility forms common in CN/JP titles, and decomposes the … ellipsis to "..."),
        /// then keeps only letters and digits, lower-cased — discarding all whitespace and punctuation.
        /// This makes matching immune to cosmetic differences in spacing, casing, smart vs. straight
        /// quotes (' ’), en/em dashes (– —), trailing "!"/"?" etc. between the JSON and the game's
        /// TROPCONF. Returns "" for null/blank input. Both sides of a match must be run through this so
        /// the comparison stays symmetric.
        /// </summary>
        private static string NormalizeTrophyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string n = name.Normalize(System.Text.NormalizationForm.FormKC);
            var sb = new System.Text.StringBuilder(n.Length);
            foreach (char c in n)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        /// <summary>
        /// Offers to relocate an imported unlock sequence onto a user-chosen start date while preserving
        /// the real gaps between unlocks — so unlock order and pacing stay identical, only the calendar
        /// position moves. Mutates <paramref name="times"/> in place. Used for donor imports (PSNProfiles
        /// scrape / JSON) whose timestamps are from older years.
        /// </summary>
        private void MaybeAnchorToStartDate(List<long> times)
        {
            var nonzero = times.Where(t => t != 0).ToList();
            if (nonzero.Count == 0)
                return;

            if (
                MessageBox.Show(
                    "Relocate this unlock sequence onto a start date you choose?\n\n"
                        + "The real time gaps between unlocks are kept — only the calendar dates move "
                        + "(use this when the donor's run is from older years).\n\n"
                        + "Yes = pick the first-unlock date.    No = keep the original dates.",
                    "Anchor to start date",
                    MessageBoxButtons.YesNo
                ) != DialogResult.Yes
            )
                return;

            dtpfForInstant.Title.Text = "First unlock: start date";
            if (dtpfForInstant.ShowDialog() != DialogResult.OK)
                return;

            // Match the unix<->DateTime convention used elsewhere (1970 UTC + seconds; timezone ignored,
            // which is fine because only the gaps and the chosen anchor matter).
            long epochAnchor = (long)(
                dtpfForInstant.dateTimePicker1.Value
                - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ).TotalSeconds;

            long firstUnlock = nonzero.Min();
            long offset = epochAnchor - firstUnlock;

            for (int i = 0; i < times.Count; i++)
                if (times[i] != 0)
                    times[i] += offset;

            long newLast = nonzero.Max() + offset;
            MessageBox.Show(
                "Sequence anchored.\n\nFirst unlock: "
                    + epochAnchor.TimeStampToDateTime().ToString(Properties.strings.DateFormatString)
                    + "\nLast unlock:  "
                    + newLast.TimeStampToDateTime().ToString(Properties.strings.DateFormatString),
                "Anchor to start date"
            );
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (copyFrom.ShowDialog(this) == DialogResult.OK)
            {
                List<long> _times;
                if (copyFrom.LocalPairs != null && copyFrom.LocalPairs.Count > 0)
                {
                    if (copyFrom.LocalPairsAreNameKeyed)
                    {
                        // Match imported timestamps to trophies by display name. Names are normalized
                        // (see NormalizeTrophyName: Unicode NFKC, collapsed whitespace, case-insensitive)
                        // so cosmetic differences between the JSON and the game's TROPCONF don't cause
                        // misses. Result is a per-trophy-index array aligned with the trophy table,
                        // exactly like the Id-keyed path.
                        var trophyIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
                        for (int i = 0; i < tconf.Count; ++i)
                        {
                            string key = NormalizeTrophyName(tconf[i].name);
                            if (key.Length > 0 && !trophyIndexByName.ContainsKey(key))
                                trophyIndexByName[key] = i;
                        }

                        int count = tusr.trophyTimeInfoTable.Count;
                        _times = Enumerable.Repeat(0L, count).ToList();

                        int matched = 0;
                        var unmatched = new List<string>();
                        foreach (var p in copyFrom.LocalPairs)
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                                continue;
                            if (trophyIndexByName.TryGetValue(NormalizeTrophyName(p.Name), out int idx) && idx < count)
                            {
                                _times[idx] = p.Date;
                                matched++;
                            }
                            else
                            {
                                unmatched.Add(p.Name);
                            }
                        }

                        // Surface anything that didn't match instead of dropping it silently, so the
                        // user can fix the JSON rather than wonder why a trophy stayed locked.
                        if (unmatched.Count > 0)
                        {
                            const int maxShown = 15;
                            string list = string.Join("\n  • ", unmatched.Take(maxShown));
                            if (unmatched.Count > maxShown)
                                list += $"\n  … and {unmatched.Count - maxShown} more";
                            MessageBox.Show(
                                $"Matched {matched} of {matched + unmatched.Count} entries by name.\n\n"
                                    + "These names matched no trophy and were skipped:\n  • "
                                    + list,
                                "Trophy name import"
                            );
                        }
                    }
                    else
                    {
                        _times = copyFrom.LocalPairs.OrderBy(p => p.Id).Select(p => p.Date).ToList();
                    }

                    // Optionally relocate the donor's unlock sequence onto a start date you pick,
                    // keeping the real gaps between unlocks (see MaybeAnchorToStartDate).
                    MaybeAnchorToStartDate(_times);
                }
                else
                    _times = copyFrom.checkBox1.Checked ? copyFrom.smartCopy().ToList() : copyFrom.copyFrom().ToList();
                // Clear stale rows first (sometimes the grid doesn't update otherwise), but only when
                // we actually have unlocks to apply — a no-match name file must not wipe everything.
                if (_times.Any(t => t != 0)) clearTrophiesMenuItem_Click(sender, e);
                try
                {
                    for (int i = 0; i < tusr.trophyTimeInfoTable.Count; ++i)
                    {

                        if (!tpsn[i].HasValue && _times[i] != 0)
                        {
                            var time = _times[i].TimeStampToDateTime();
                            tusr.UnlockTrophy(i, time);
                            tpsn.PutTrophy(i, tusr.trophyTypeTable[i].Type, time);
                        }
                    }
                    haveBeenEdited = true;
                    RefreshComponents();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }

        private void menuStrip1_Click(object sender, EventArgs e)
        {
            if (listViewEx1.IsEditing)
                listViewEx1.EndEditing(true);
        }
        private void toggleRPCS3TrophyFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isRpcs3Format.Checked = !isRpcs3Format.Checked;
        }

        #region ListView column sorting and color legend (UI enhancements)

        private int _sortColumn = -1;
        private SortOrder _sortOrder = SortOrder.Ascending;

        /// <summary>
        /// Sorts the trophy list by the clicked column, toggling ascending/descending on repeat clicks,
        /// and shows the matching arrow in the header. Row identity is unaffected because every row
        /// carries its trophy id in <see cref="ListViewItem.ImageIndex"/>, not its visual position.
        /// </summary>
        private void listViewEx1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }

            listViewEx1.ListViewItemSorter = new ListViewItemComparer(e.Column, _sortOrder);
            listViewEx1.Sort();
            SetSortArrow(_sortColumn, _sortOrder);
        }

        /// <summary>
        /// Compares two rows by one column's text. Values that both parse as numbers compare numerically;
        /// everything else (including the "yyyy/MM/dd HH:mm:ss" time column, which sorts chronologically
        /// as text) compares as a culture-aware string.
        /// </summary>
        private sealed class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int _column;
            private readonly SortOrder _order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                _column = column;
                _order = order;
            }

            public int Compare(object x, object y)
            {
                string a = TextOf((ListViewItem)x);
                string b = TextOf((ListViewItem)y);

                int result;
                if (double.TryParse(a, out double da) && double.TryParse(b, out double db))
                    result = da.CompareTo(db);
                else
                    result = string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);

                return _order == SortOrder.Descending ? -result : result;
            }

            private string TextOf(ListViewItem item)
            {
                return _column < item.SubItems.Count ? item.SubItems[_column].Text : string.Empty;
            }
        }

        // --- Native sort-arrow glyph on the list's header (no managed WinForms API exists for this) ---
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETHEADER = LVM_FIRST + 31;
        private const int HDM_FIRST = 0x1200;
        private const int HDM_GETITEM = HDM_FIRST + 11;
        private const int HDM_SETITEM = HDM_FIRST + 12;
        private const int HDI_FORMAT = 0x0004;
        private const int HDF_SORTUP = 0x0400;
        private const int HDF_SORTDOWN = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct HDITEM
        {
            public int mask;
            public int cxy;
            public IntPtr pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public IntPtr lParam;
            public int iImage;
            public int iOrder;
            public uint type;
            public IntPtr pvFilter;
            public uint state;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref HDITEM lParam);

        /// <summary>
        /// Puts the up/down sort glyph on <paramref name="sortColumn"/>'s header and clears it from every
        /// other column.
        /// </summary>
        private void SetSortArrow(int sortColumn, SortOrder order)
        {
            if (!listViewEx1.IsHandleCreated)
                return;

            IntPtr header = SendMessage(listViewEx1.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (header == IntPtr.Zero)
                return;

            for (int i = 0; i < listViewEx1.Columns.Count; i++)
            {
                var hd = new HDITEM { mask = HDI_FORMAT };
                SendMessage(header, HDM_GETITEM, (IntPtr)i, ref hd);

                hd.fmt &= ~(HDF_SORTUP | HDF_SORTDOWN);
                if (i == sortColumn && order != SortOrder.None)
                    hd.fmt |= order == SortOrder.Ascending ? HDF_SORTUP : HDF_SORTDOWN;

                SendMessage(header, HDM_SETITEM, (IntPtr)i, ref hd);
            }
        }

        /// <summary>
        /// Builds the bottom-docked color legend explaining the row background colors set in
        /// <see cref="RefreshComponents"/> (white = unlocked, pink = synced, gray = still locked).
        /// The list view is Dock=Fill, so docking this to the bottom shrinks the list to fit.
        /// </summary>
        private void BuildColorLegend()
        {
            var legend = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(6, 3, 6, 3),
                BackColor = SystemColors.Control,
            };

            legend.Controls.Add(MakeLegendEntry(Color.White, "Unlocked"));
            legend.Controls.Add(MakeLegendEntry(Color.LightPink, "Synced"));
            legend.Controls.Add(MakeLegendEntry(Color.LightGray, "Locked"));

            Controls.Add(legend);
            legend.BringToFront();
        }

        private static Control MakeLegendEntry(Color color, string text)
        {
            var entry = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2, 0, 14, 0),
            };
            entry.Controls.Add(new Label
            {
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(14, 14),
                Margin = new Padding(0, 2, 5, 0),
            });
            entry.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0),
            });
            return entry;
        }

        /// <summary>
        /// Computes, for every unlocked trophy, the PSNProfiles-style "elapsed since the first trophy
        /// (+gap from the previous trophy)" string. Both are measured in chronological unlock order,
        /// which is independent of the grid's current sort. The first trophy gets "", the second gets
        /// just the elapsed value (gap equals elapsed there), and the rest get "elapsed (+gap)".
        /// Keyed by trophy id (== ListViewItem.ImageIndex).
        /// </summary>
        private Dictionary<int, string> ComputeTimeDiffStrings()
        {
            var unlocked = new List<KeyValuePair<int, DateTime>>();
            if (tconf != null && tpsn != null && tusr != null)
            {
                for (int i = 0; i < tconf.Count; i++)
                {
                    DateTime? t = UnlockTimeOf(i);
                    if (t.HasValue)
                        unlocked.Add(new KeyValuePair<int, DateTime>(i, t.Value));
                }
            }
            unlocked.Sort((a, b) => a.Value.CompareTo(b.Value));

            var result = new Dictionary<int, string>();
            for (int k = 0; k < unlocked.Count; k++)
            {
                if (k == 0)
                {
                    result[unlocked[k].Key] = string.Empty;
                    continue;
                }

                TimeSpan elapsed = unlocked[k].Value - unlocked[0].Value;
                if (k == 1)
                {
                    result[unlocked[k].Key] = FormatSpan(elapsed);
                }
                else
                {
                    TimeSpan gap = unlocked[k].Value - unlocked[k - 1].Value;
                    result[unlocked[k].Key] = FormatSpan(elapsed) + " (+" + FormatSpan(gap) + ")";
                }
            }
            return result;
        }

        /// <summary>
        /// The unlock timestamp the Time column shows for trophy <paramref name="i"/>, or null if it
        /// isn't unlocked. Mirrors the source-of-truth precedence used in <see cref="RefreshComponents"/>.
        /// </summary>
        private DateTime? UnlockTimeOf(int i)
        {
            if (tpsn[i].HasValue && tpsn[i].Value.Time.Ticks > 0)
                return tpsn[i].Value.Time;
            DateTime t = tusr.trophyTimeInfoTable[i].Time;
            return t.Ticks > 0 ? t : (DateTime?)null;
        }

        /// <summary>Compact span formatter, e.g. "1h 18m 5s", "43m 14s", "9s". Days are shown when present.</summary>
        private static string FormatSpan(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero;
            var sb = new System.Text.StringBuilder();
            if (ts.Days > 0)
                sb.Append(ts.Days).Append("d ");
            if (ts.Hours > 0)
                sb.Append(ts.Hours).Append("h ");
            if (ts.Minutes > 0)
                sb.Append(ts.Minutes).Append("m ");
            sb.Append(ts.Seconds).Append('s');
            return sb.ToString();
        }

        /// <summary>
        /// Recomputes the time-difference column in place for all rows. Called after single-row edits
        /// (unlock / change time / delete) that don't trigger a full <see cref="RefreshComponents"/>,
        /// since changing one trophy's time shifts the elapsed/gap values of every later trophy.
        /// </summary>
        private void RefreshTimeDiffColumn()
        {
            if (tconf == null)
                return;

            int col = columnHeader9.Index;
            var diffs = ComputeTimeDiffStrings();
            foreach (ListViewItem item in listViewEx1.Items)
            {
                if (col < item.SubItems.Count)
                    item.SubItems[col].Text =
                        diffs.TryGetValue(item.ImageIndex, out string s) ? s : string.Empty;
            }
        }

        #endregion
    }
}
