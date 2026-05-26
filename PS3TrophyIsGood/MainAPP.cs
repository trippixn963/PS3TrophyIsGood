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
        private Label emptyHint; // "open/drag a folder" overlay shown when no game is loaded
        private ToolStrip toolbar; // modern flat toolbar replacing the old menu bar
        private Panel heroPanel; // header: game icon, title, completion ring
        private PictureBox gameIcon;
        private Label gameTitle;
        private Label gameSubtitle;
        private UI.RingControl completionRing;

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
            UI.Theme.Apply(this);
            listViewEx1.OwnerDraw = true; // dark-render the column headers (WinForms headers ignore BackColor)
            listViewEx1.DrawColumnHeader += listViewEx1_DrawColumnHeader;
            listViewEx1.DrawItem += listViewEx1_DrawItem;
            listViewEx1.DrawSubItem += listViewEx1_DrawSubItem;
            // The list (Dock=Fill) covers the form, so make IT a drop target too — relying on the drop
            // bubbling up to the form is unreliable. Reuses the form's (sender-agnostic) drag handlers.
            listViewEx1.AllowDrop = true;
            listViewEx1.DragEnter += Form1_DragEnter;
            listViewEx1.DragDrop += Form1_DragDrop;
            // Center the short status columns for a cleaner grid (Name/Detail/Time/gap stay left).
            columnHeader3.TextAlign = HorizontalAlignment.Center; // Type
            columnHeader4.TextAlign = HorizontalAlignment.Center; // Hidden
            columnHeader5.TextAlign = HorizontalAlignment.Center; // Got
            columnHeader7.TextAlign = HorizontalAlignment.Center; // Synced
            columnHeader8.TextAlign = HorizontalAlignment.Center; // From
            BuildColorLegend();
            BuildShell();
            // The old top-corner stat labels, progress bar and link are replaced by the toolbar + hero.
            label1.Visible = label2.Visible = label3.Visible = label4.Visible = false;
            progressBar1.Visible = false;
            linkLabel1.Visible = false;
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

            // QoL: re-open the folder that was open at last exit, if it still exists.
            string lastPath = Properties.Settings.Default.LastOpenedPath;
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                OpenFile(lastPath);
                if (!isOpen)
                {
                    // Folder exists but no longer opens as trophy data — forget it so it won't retry.
                    Properties.Settings.Default.LastOpenedPath = string.Empty;
                    Properties.Settings.Default.Save();
                }
            }

            UpdateEmptyHint();
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
                UI.Dialog.Show(string.Format(Properties.strings.PsnSyncTime, lastSyncTrophyTime.ToString(Properties.strings.DateFormatString)));
                return false;
            }
            return true;
        }

        private void DeleteTrophy(int trophyId, ListViewItem lvi)
        {
            if (IsTrophySync(trophyId))
            {
                UI.Dialog.Show(Properties.strings.SyncedTrophyCanNotEdit);
            }
            else
            if (trophyId != 0 && tconf[trophyId].gid == 0 && IsTrophyGot(0))
            {
                UI.Dialog.Show(Properties.strings.CantLoclPlatinumBeforOther);
            }
            else
            if (UI.Dialog.Show(Properties.strings.DeleteTrophyConfirm, Properties.strings.Delete, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                tpsn.DeleteTrophyByID(trophyId);
                tusr.LockTrophy(trophyId);
                lvi.SubItems[4].Text = Properties.strings.no;
                lvi.BackColor = UI.Theme.RowLockedBack;
                lvi.ForeColor = UI.Theme.RowLockedText;
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
                UI.Dialog.Show(Properties.strings.CantUnloclPlatinumBeforOther);
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
                        lvi.BackColor = UI.Theme.RowUnlockedBack;
                        lvi.ForeColor = UI.Theme.RowUnlockedText;
                        lvi.SubItems[6].Text = trophyTime.ToString(Properties.strings.DateFormatString);
                        CompletionRates();
                        RefreshTimeDiffColumn();
                        haveBeenEdited = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        UI.Dialog.Show(ex.Message);
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
                UI.Dialog.Show(Properties.strings.SyncedTrophyCanNotEdit);
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
                        UI.Dialog.Show(ex.Message);
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
                    if (tpsn[i].Value.IsSync)
                    {
                        lvi.BackColor = UI.Theme.RowSyncedBack;
                        lvi.ForeColor = UI.Theme.RowSyncedText;
                    }
                    else
                    {
                        lvi.BackColor = UI.Theme.RowUnlockedBack;
                        lvi.ForeColor = UI.Theme.RowUnlockedText;
                    }
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
                    {
                        lvi.BackColor = UI.Theme.RowSyncedBack;
                        lvi.ForeColor = UI.Theme.RowSyncedText;
                    }
                    else if (tusr.trophyTimeInfoTable[i].IsGet)
                    {
                        lvi.BackColor = UI.Theme.RowUnlockedBack;
                        lvi.ForeColor = UI.Theme.RowUnlockedText;
                    }
                    else
                    {
                        lvi.BackColor = UI.Theme.RowLockedBack;
                        lvi.ForeColor = UI.Theme.RowLockedText;
                    }
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
            UpdateEmptyHint();
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
            if (gameTitle != null)
                gameTitle.Text = string.Empty;
            if (gameSubtitle != null)
                gameSubtitle.Text = string.Empty;
            if (completionRing != null)
                completionRing.Percent = 0;
            if (gameIcon != null)
                gameIcon.Image = null;
            UpdateEmptyHint();
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

            int pct = totalGrade > 0 ? (int)System.Math.Round(getGrade * 100.0 / totalGrade) : 0;
            if (gameTitle != null)
                gameTitle.Text = tconf.title_name;
            if (gameSubtitle != null)
                gameSubtitle.Text =
                    $"{isGetTrophyNumber} / {tconf.Count} trophies      {getGrade} / {totalGrade} pts";
            if (completionRing != null)
                completionRing.Percent = pct;
            LoadGameIcon();
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
                // Use the row being edited, not SelectedItems[0]: the selection can be empty here when
                // the edit ends because focus left the list (e.g. clicking a column header to sort).
                ListViewItem lvi = e.Item;
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
            if (((ListView)sender).SelectedItems.Count == 0)
                return; // double-clicked empty space — nothing to edit
            int trophyID = ((ListView)sender).SelectedItems[0].ImageIndex;// Note: the ListView ImageIndex doubles as the trophy ID (e.g. Platinum = 0, 1...)
            ListViewItem lvi = ((ListView)sender).SelectedItems[0];
            if (IsTrophySync(trophyID))
            { // only un-synced trophies can be edited
                UI.Dialog.Show(Properties.strings.SyncedTrophyCanNotEdit);
            }
            else if (tpsn[trophyID].HasValue || (isRpcs3Format.Checked && IsTrophyGot(trophyID)))
            {
                DeleteTrophy(trophyID, lvi);
            }
            else
            {  // nonget
                if (trophyID == 0 && tconf.HasPlatinium && (GetCountBaseTrophiesGot() < baseGameCount))
                {
                    UI.Dialog.Show(Properties.strings.CantUnloclPlatinumBeforOther);
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
            if (CloseFile())
            {
                // Explicit close means "forget it" — don't auto-reopen this folder next launch.
                Properties.Settings.Default.LastOpenedPath = string.Empty;
                Properties.Settings.Default.Save();
            }
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

                // Remember this folder so it can be re-opened automatically on the next launch.
                Properties.Settings.Default.LastOpenedPath = path;
                Properties.Settings.Default.Save();
            }
            catch (FileNotFoundException ex)
            {
                tconf = null;
                tpsn = null;
                tusr = null;
                GC.Collect();
                UI.Dialog.Show(string.Format(Properties.strings.FileNotFoundMsg, Path.GetFileName(ex.FileName)));
            }
            catch (Exception ex)
            {
                tconf = null;
                tpsn = null;
                tusr = null;
                GC.Collect();
                Console.WriteLine(ex.StackTrace);
                UI.Dialog.Show(ex.Message);
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
                DialogResult dr = UI.Dialog.Show(Properties.strings.CloseConfirm, Properties.strings.Close, MessageBoxButtons.YesNoCancel);
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
            UI.Dialog.Show(Properties.strings.RestartProgram);
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
        /// Rebuilds an imported unlock sequence as realistic nightly play sessions spanning the user's
        /// chosen start date through today, finishing with the platinum earned today:
        ///  • The sequence is split into nightly sessions (a few hours each). The FIRST session lands on
        ///    the chosen start date; the platinum is "just earned" — the final session ends a few minutes
        ///    before NOW (so syncing right after looks like a fresh plat, never in the future); the middle
        ///    sessions are spread across the nights between, days off filling the rest.
        ///  • Each session sits in the overnight slot (~10pm onward), never the active daytime hours.
        ///  • Burst pops (≤60s — stacks / story trophies, and the platinum's pop) keep their EXACT scraped
        ///    gaps (strict rule, 100% match). Every other gap is the donor's gap PLUS a few minutes
        ///    (occasionally a longer lull) — always SLOWER than the donor, never matching their spacing.
        /// Mutates <paramref name="times"/> in place.
        /// </summary>
        private void MaybeRelocateToNightSessions(List<long> times)
        {
            const int SessionStartHour = 22; // sessions begin ~10pm — change to move the nightly window
            const int NightStartJitterMinutes = 75; // start spread over [10:00pm, 11:15pm]
            const int MinSessionMinutes = 150; // each night holds ~2.5–5 h of play
            const int MaxSessionMinutes = 300;
            const long BurstGapSeconds = 60; // gaps this small are stacks / story pops — kept exact
            const int MinExtraMinutes = 1; //   non-burst gap = donor's gap + this many minutes (never faster)
            const int MaxExtraMinutes = 10;
            const int LullChancePercent = 12;
            const int LullMinMinutes = 15;
            const int LullMaxMinutes = 50;

            var nonzero = times.Where(t => t != 0).ToList();
            if (nonzero.Count == 0)
                return;

            if (
                UI.Dialog.Show(
                    "Rebuild this run as nightly play sessions spanning your start date through today, "
                        + "finishing with the platinum earned today?\n\n"
                        + "The first session lands on the date you pick; the platinum lands today; the rest "
                        + "is spread across the nights between, with days off. Bursts stay exact; every other "
                        + "gap is the donor's gap plus a few minutes (always SLOWER than the donor).\n\n"
                        + "Yes = pick the start date.    No = keep the original dates.",
                    "Relocate to night sessions",
                    MessageBoxButtons.YesNo
                ) != DialogResult.Yes
            )
                return;

            dtpfForInstant.Title.Text = "Start date — the first night of the run";
            if (dtpfForInstant.ShowDialog() != DialogResult.OK)
                return;
            DateTime startDate = dtpfForInstant.dateTimePicker1.Value.Date;
            if (startDate > DateTime.Today)
                startDate = DateTime.Today; // a run can't start in the future

            // Earned trophies in chronological order.
            var original = new List<long>(times);
            var seq = new List<KeyValuePair<int, long>>();
            for (int i = 0; i < original.Count; i++)
                if (original[i] != 0)
                    seq.Add(new KeyValuePair<int, long>(i, original[i]));
            seq.Sort((a, b) => a.Value.CompareTo(b.Value));

            var rand = new Random();
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ToUnix(DateTime dt) => (long)(dt - epoch).TotalSeconds;

            // 1) Split into sessions; record each trophy's offset (seconds) from its session start. Bursts
            //    keep the donor's exact gap; non-burst gaps are the donor's gap plus a few minutes (rarely
            //    a longer lull) — always slower. A new session starts when a night's play time fills up
            //    (never splitting a burst).
            var sessionStart = new List<int> { 0 }; // seq indices that begin a session
            var relOffset = new long[seq.Count];
            long elapsed = 0;
            long nightLenSec = (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
            for (int k = 1; k < seq.Count; k++)
            {
                long gap = seq[k].Value - seq[k - 1].Value;
                long add;
                if (gap <= BurstGapSeconds)
                {
                    add = gap; // exact burst — never starts a new session
                }
                else
                {
                    long extraMin = rand.Next(MinExtraMinutes, MaxExtraMinutes + 1);
                    if (rand.Next(100) < LullChancePercent)
                        extraMin += rand.Next(LullMinMinutes, LullMaxMinutes + 1);
                    add = gap + extraMin * 60 + rand.Next(0, 60);

                    if (elapsed + add > nightLenSec)
                    {
                        sessionStart.Add(k); // begin a new night
                        relOffset[k] = 0;
                        elapsed = 0;
                        nightLenSec = (long)rand.Next(MinSessionMinutes, MaxSessionMinutes + 1) * 60;
                        continue;
                    }
                }
                relOffset[k] = relOffset[k - 1] + add;
                elapsed += add;
            }
            int sessions = sessionStart.Count;

            // 2) Assign each session a calendar night. The final (platinum) session is anchored to today's
            //    early morning below; the non-final sessions get distinct nights spread across
            //    [startDate, today-2] so their post-midnight tails can't collide with the final session.
            var nightDay = new DateTime[sessions];
            int lead = sessions - 1; // non-final session count
            if (lead >= 1)
            {
                // First non-final session is the chosen start date; the rest land on RANDOM distinct days
                // in (startDate, today-2] — random, not evenly spaced, so play nights look human rather
                // than metronomic. (today-1/today are reserved for the final session's overnight tail.)
                nightDay[0] = startDate;
                int availDays = Math.Max(0, (int)(DateTime.Today.AddDays(-2) - startDate).TotalDays);
                if (lead - 1 <= availDays)
                {
                    var offsets = new List<int>();
                    for (int d = 1; d <= availDays; d++)
                        offsets.Add(d);
                    for (int i = offsets.Count - 1; i > 0; i--) // Fisher–Yates shuffle
                    {
                        int j = rand.Next(i + 1);
                        int tmp = offsets[i];
                        offsets[i] = offsets[j];
                        offsets[j] = tmp;
                    }
                    var chosen = offsets.Take(lead - 1).ToList();
                    chosen.Sort();
                    for (int s = 1; s < lead; s++)
                        nightDay[s] = startDate.AddDays(chosen[s - 1]);
                }
                else
                {
                    // Too many sessions for the window — consecutive nights ending two days ago (the run
                    // then begins earlier than the chosen start; can't be helped without going faster).
                    for (int s = 0; s < lead; s++)
                        nightDay[s] = DateTime.Today.AddDays(-2 - (lead - 1 - s));
                }
            }

            // 3) Place the non-final sessions forward from ~10pm on their night.
            for (int s = 0; s < lead; s++)
            {
                int from = sessionStart[s];
                int to = sessionStart[s + 1] - 1;
                DateTime ns = nightDay[s]
                    .AddHours(SessionStartHour)
                    .AddMinutes(rand.Next(0, NightStartJitterMinutes + 1))
                    .AddSeconds(rand.Next(0, 60)); // real sessions don't begin exactly on the :00 second
                for (int k = from; k <= to; k++)
                    times[seq[k].Key] = ToUnix(ns.AddSeconds(relOffset[k]));
            }

            // 4) Final session: the platinum is "just earned". Anchor the LAST trophy to right NOW (minus
            //    a small buffer), so it looks like the user finished an offline session moments ago and is
            //    about to sync. The session leads up to it. Never in the future.
            {
                int from = sessionStart[lead];
                int last = seq.Count - 1;
                long finalDuration = relOffset[last];
                DateTime platTarget = DateTime.Now.AddSeconds(-rand.Next(30, 301)); // earned ~0.5–5 min ago
                DateTime finalStart = platTarget.AddSeconds(-finalDuration);
                for (int k = from; k <= last; k++)
                    times[seq[k].Key] = ToUnix(finalStart.AddSeconds(relOffset[k]));
            }

            // 5) The platinum's pop gap must match the donor EXACTLY (strict rule).
            bool platEarned = tconf != null && tconf.HasPlatinium && original.Count > 0 && original[0] != 0;
            if (platEarned)
            {
                long platOrig = original[0];
                int prevIdx = -1;
                long prevOrig = long.MinValue;
                for (int i = 1; i < original.Count; i++)
                    if (original[i] != 0 && original[i] <= platOrig && original[i] > prevOrig)
                    {
                        prevOrig = original[i];
                        prevIdx = i;
                    }
                if (prevIdx >= 0)
                    times[0] = times[prevIdx] + (platOrig - prevOrig);
            }

            // HARD RULE: nothing may ever be dated after "now". Syncing a future-dated trophy to PSN gets
            // the account flagged and removed from leaderboards. As a final guarantee, pull the whole run
            // back whole days (preserving the night times) until the latest pop is at/just before now.
            long nowUnix = ToUnix(DateTime.Now);
            long maxT = times.Where(t => t != 0).Max();
            while (maxT > nowUnix)
            {
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != 0)
                        times[i] -= 24L * 3600L;
                maxT -= 24L * 3600L;
            }

            long firstUnlock = times.Where(t => t != 0).Min();
            long lastUnlock = times.Where(t => t != 0).Max();
            UI.Dialog.Show(
                $"Rebuilt across {sessions} session(s) — slower than the donor, never a 1:1 copy, "
                    + (platEarned ? "platinum" : "last trophy")
                    + " earned just now.\n\nStarted:   "
                    + firstUnlock.TimeStampToDateTime().ToString(Properties.strings.DateFormatString)
                    + "\nFinished:  "
                    + lastUnlock.TimeStampToDateTime().ToString(Properties.strings.DateFormatString),
                "Relocate to night sessions"
            );
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (copyFrom.ShowDialog(this) != DialogResult.OK)
                return;
            if (copyFrom.LocalPairs == null || copyFrom.LocalPairs.Count == 0)
                return;

            // Match the scraped trophies to the loaded game by display name. Names are normalized
            // (see NormalizeTrophyName: Unicode NFKC, collapsed whitespace, case-insensitive) so cosmetic
            // differences between PSNProfiles and the game's TROPCONF don't cause misses. Result is a
            // per-trophy-index array aligned with the trophy table.
            var trophyIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < tconf.Count; ++i)
            {
                string key = NormalizeTrophyName(tconf[i].name);
                if (key.Length > 0 && !trophyIndexByName.ContainsKey(key))
                    trophyIndexByName[key] = i;
            }

            int count = tusr.trophyTimeInfoTable.Count;
            var _times = Enumerable.Repeat(0L, count).ToList();

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

            // Surface anything that didn't match instead of dropping it silently.
            if (unmatched.Count > 0)
            {
                const int maxShown = 15;
                string list = string.Join("\n  • ", unmatched.Take(maxShown));
                if (unmatched.Count > maxShown)
                    list += $"\n  … and {unmatched.Count - maxShown} more";
                UI.Dialog.Show(
                    $"Matched {matched} of {matched + unmatched.Count} scraped trophies by name.\n\n"
                        + "These names matched no trophy and were skipped:\n  • "
                        + list,
                    "PSNProfiles import"
                );
            }

            // Rebuild the unlock sequence as realistic nightly play sessions (see MaybeRelocateToNightSessions).
            MaybeRelocateToNightSessions(_times);

            // Clear stale rows first (sometimes the grid doesn't update otherwise), but only when we
            // actually have unlocks to apply — a no-match scrape must not wipe everything.
            if (_times.Any(t => t != 0))
                clearTrophiesMenuItem_Click(sender, e);
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
                UI.Dialog.Show(ex.Message);
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
            listViewEx1.Invalidate(); // redraw the owner-drawn headers so the sort arrow moves
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

        // --- Dark owner-drawn column headers (WinForms ListView headers ignore BackColor) ---
        private void listViewEx1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var bg = new SolidBrush(UI.Theme.Panel))
                e.Graphics.FillRectangle(bg, e.Bounds);
            using (var pen = new Pen(UI.Theme.Border))
                e.Graphics.DrawLine(
                    pen,
                    e.Bounds.Left,
                    e.Bounds.Bottom - 1,
                    e.Bounds.Right,
                    e.Bounds.Bottom - 1
                );

            TextFormatFlags hAlign;
            switch (e.Header.TextAlign)
            {
                case HorizontalAlignment.Center:
                    hAlign = TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    hAlign = TextFormatFlags.Right;
                    break;
                default:
                    hAlign = TextFormatFlags.Left;
                    break;
            }
            var textBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                UI.Theme.UiFont,
                textBounds,
                UI.Theme.Text,
                hAlign | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );

            if (e.ColumnIndex == _sortColumn)
            {
                string glyph = _sortOrder == SortOrder.Descending ? "▼" : "▲";
                var arrowBounds = new Rectangle(e.Bounds.Right - 18, e.Bounds.Y, 16, e.Bounds.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    glyph,
                    UI.Theme.UiFont,
                    arrowBounds,
                    UI.Theme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            }
        }

        // Full owner-draw of the rows: PlayStation-blue selection, per-state text colour, and a
        // metal-coloured Type cell. In Details view every cell is painted in DrawSubItem.
        private void listViewEx1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Intentionally empty — DrawSubItem paints each cell (which tiles the whole row).
        }

        private void listViewEx1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            Color back = selected ? UI.Theme.Accent : e.Item.BackColor;
            using (var b = new SolidBrush(back))
                e.Graphics.FillRectangle(b, e.Bounds);

            Color fore = selected ? Color.White : e.Item.ForeColor;

            // Column 0 = trophy icon + name.
            if (e.ColumnIndex == 0)
            {
                int x = e.Bounds.X + 4;
                var imgs = listViewEx1.SmallImageList;
                if (imgs != null && e.Item.ImageIndex >= 0 && e.Item.ImageIndex < imgs.Images.Count)
                {
                    int sz = imgs.ImageSize.Height;
                    int y = e.Bounds.Y + (e.Bounds.Height - sz) / 2;
                    e.Graphics.DrawImage(imgs.Images[e.Item.ImageIndex], new Rectangle(x, y, sz, sz));
                    x += sz + 8;
                }
                var nameRect = new Rectangle(x, e.Bounds.Y, e.Bounds.Right - x - 4, e.Bounds.Height);
                TextRenderer.DrawText(
                    e.Graphics, e.Item.Text, UI.Theme.UiFont, nameRect, fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );
                return;
            }

            // Type column gets its metal colour (unless the row is selected → keep white for contrast).
            if (e.ColumnIndex == 2 && !selected)
                fore = MetalColor(e.SubItem.Text);

            TextFormatFlags hAlign =
                e.Header.TextAlign == HorizontalAlignment.Center
                    ? TextFormatFlags.HorizontalCenter
                    : e.Header.TextAlign == HorizontalAlignment.Right
                        ? TextFormatFlags.Right
                        : TextFormatFlags.Left;
            var cellRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics, e.SubItem.Text, UI.Theme.UiFont, cellRect, fore,
                hAlign | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }

        /// <summary>Maps a PS3 trophy-type letter (P/G/S/B) to its metal colour.</summary>
        private static Color MetalColor(string ttype)
        {
            if (string.IsNullOrEmpty(ttype))
                return UI.Theme.Text;
            switch (char.ToUpperInvariant(ttype[0]))
            {
                case 'P':
                    return Color.FromArgb(0x6F, 0xC8, 0xF0); // platinum — cyan-blue
                case 'G':
                    return Color.FromArgb(0xF0, 0xC4, 0x40); // gold
                case 'S':
                    return Color.FromArgb(0xC4, 0xCC, 0xD4); // silver
                case 'B':
                    return Color.FromArgb(0xCD, 0x7F, 0x32); // bronze
                default:
                    return UI.Theme.Text;
            }
        }

        /// <summary>
        /// Builds the modern shell: a flat toolbar (replacing the old menu bar, reusing every existing
        /// action handler) and a hero header (game icon, title, completion ring). Added after the list and
        /// bottom bar so they dock to the top; the old menu strip is hidden.
        /// </summary>
        private void BuildShell()
        {
            toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new UI.DarkToolStripRenderer(),
                BackColor = UI.Theme.Panel,
                ForeColor = UI.Theme.Text,
                Padding = new Padding(6, 4, 6, 4),
                AutoSize = false,
                Height = 38,
            };

            ToolStripButton Btn(string text, EventHandler onClick)
            {
                var b = new ToolStripButton(text)
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    ForeColor = UI.Theme.Text,
                    Padding = new Padding(6, 2, 6, 2),
                };
                b.Click += onClick;
                return b;
            }
            toolbar.Items.Add(Btn("📂  Open", openMenuItem_Click));
            toolbar.Items.Add(Btn("⭳  Copy from PSNProfiles", toolStripMenuItem1_Click));
            toolbar.Items.Add(Btn("💾  Save", saveMenuItem_Click));
            toolbar.Items.Add(Btn("⟳  Refresh", refreshMenuItem_Click));
            toolbar.Items.Add(new ToolStripSeparator());

            var more = new ToolStripDropDownButton("More  ▾")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = UI.Theme.Text,
                ShowDropDownArrow = false,
            };
            ToolStripMenuItem Item(string text, EventHandler onClick)
            {
                var mi = new ToolStripMenuItem(text);
                mi.Click += onClick;
                return mi;
            }
            more.DropDownItems.Add(Item("Instant Platinum", instantPlatinumMenuItem_Click));
            more.DropDownItems.Add(Item("Clear Trophies", clearTrophiesMenuItem_Click));
            more.DropDownItems.Add(new ToolStripSeparator());
            more.DropDownItems.Add(Item("Set Random Start Time", setRandomStartTimeToolStripMenuItem_Click));
            more.DropDownItems.Add(Item("Set Random End Time", setRandomEndTimeToolStripMenuItem_Click));
            more.DropDownItems.Add(new ToolStripSeparator());
            var rpcs3Item = new ToolStripMenuItem("RPCS3 Trophy Format") { Checked = isRpcs3Format.Checked };
            rpcs3Item.Click += (s, e) =>
            {
                toggleRPCS3TrophyFormatToolStripMenuItem_Click(s, e);
                rpcs3Item.Checked = isRpcs3Format.Checked;
            };
            more.DropDownItems.Add(rpcs3Item);
            more.DropDownItems.Add(new ToolStripSeparator());
            more.DropDownItems.Add(Item("Close File", closeFileMenuItem_Click));
            more.DropDownItems.Add(Item("View on GitHub", (s, e) => linkLabel1_LinkClicked(s, null)));
            more.DropDownItems.Add(Item("Exit", exitMenuItem_Click));
            more.DropDown.BackColor = UI.Theme.Panel;
            more.DropDown.ForeColor = UI.Theme.Text;
            ((ToolStripDropDownMenu)more.DropDown).Renderer = new UI.DarkToolStripRenderer();
            toolbar.Items.Add(more);

            // Move the profile + language pickers onto the toolbar, right-aligned.
            menuStrip1.Items.Remove(toolStripComboBox2);
            menuStrip1.Items.Remove(toolStripComboBox1);
            toolStripComboBox1.Alignment = ToolStripItemAlignment.Right;
            toolStripComboBox2.Alignment = ToolStripItemAlignment.Right;
            toolbar.Items.Add(toolStripComboBox1);
            toolbar.Items.Add(toolStripComboBox2);

            // --- Hero header ---
            heroPanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = UI.Theme.Surface };
            heroPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UI.Theme.Border))
                    e.Graphics.DrawLine(pen, 0, heroPanel.Height - 1, heroPanel.Width, heroPanel.Height - 1);
            };
            gameIcon = new PictureBox
            {
                Location = new Point(16, 12),
                Size = new Size(56, 56),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = UI.Theme.Surface,
            };
            gameTitle = new Label
            {
                Location = new Point(86, 14),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = UI.Theme.Text,
                BackColor = Color.Transparent,
            };
            gameSubtitle = new Label
            {
                Location = new Point(88, 46),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = UI.Theme.TextMuted,
                BackColor = Color.Transparent,
            };
            completionRing = new UI.RingControl { Size = new Size(58, 58) };
            heroPanel.Controls.Add(gameIcon);
            heroPanel.Controls.Add(gameTitle);
            heroPanel.Controls.Add(gameSubtitle);
            heroPanel.Controls.Add(completionRing);
            heroPanel.Resize += (s, e) =>
                completionRing.Location = new Point(heroPanel.Width - completionRing.Width - 18, 11);

            // Hide the old menu; add hero then toolbar last so the toolbar docks to the very top.
            menuStrip1.Visible = false;
            Controls.Add(heroPanel);
            Controls.Add(toolbar);
        }

        /// <summary>Loads the game's ICON0.PNG into the hero (a copy, so the file isn't locked).</summary>
        private void LoadGameIcon()
        {
            if (gameIcon == null)
                return;
            try
            {
                string iconPath = System.IO.Path.Combine(path ?? string.Empty, "ICON0.PNG");
                if (System.IO.File.Exists(iconPath))
                {
                    using (var img = Image.FromFile(iconPath))
                        gameIcon.Image = new Bitmap(img);
                    return;
                }
            }
            catch { /* fall through to no icon */ }
            gameIcon.Image = null;
        }

        /// <summary>Builds the bottom color-legend bar (white = unlocked, rose = synced, dim = locked).</summary>
        private void BuildColorLegend()
        {
            var legend = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(10, 4, 8, 4),
                BackColor = UI.Theme.Panel,
            };
            legend.Controls.Add(MakeLegendEntry(UI.Theme.RowUnlockedBack, "Unlocked"));
            legend.Controls.Add(MakeLegendEntry(UI.Theme.RowSyncedBack, "Synced"));
            legend.Controls.Add(MakeLegendEntry(UI.Theme.RowLockedBack, "Locked"));
            Controls.Add(legend);
            legend.BringToFront();

            // Empty-state overlay: shown over the (Dock=Fill) list when no game is loaded.
            emptyHint = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = UI.Theme.TextMuted,
                BackColor = UI.Theme.Surface,
                Font = new Font("Segoe UI", 11F),
                Text = "Open a trophy folder, or drag one here",
            };
            Controls.Add(emptyHint);
            emptyHint.BringToFront();
        }

        /// <summary>Shows the empty-state overlay only when no trophies are loaded.</summary>
        private void UpdateEmptyHint()
        {
            if (emptyHint == null)
                return;
            emptyHint.Visible = listViewEx1.Items.Count == 0;
            if (emptyHint.Visible)
                emptyHint.BringToFront();
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
                ForeColor = UI.Theme.Text,
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
