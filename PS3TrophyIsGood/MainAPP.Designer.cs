namespace PS3TrophyIsGood
{
    partial class MainAPP
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainAPP));
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.isRpcs3Format = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.advancedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.instantPlatinumMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearTrophiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.setRandomStartTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setRandomEndTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripComboBox2 = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.listViewEx1 = new ListViewEx.ListViewEx();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // linkLabel1
            // 
            resources.ApplyResources(this.linkLabel1, "linkLabel1");
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.TabStop = true;
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.imageList1, "imageList1");
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenuItem,
            this.advancedMenuItem,
            this.refreshMenuItem,
            this.toolStripComboBox2,
            this.toolStripComboBox1});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Click += new System.EventHandler(this.menuStrip1_Click);
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMenuItem,
            this.saveMenuItem,
            this.closeFileMenuItem,
            this.isRpcs3Format,
            this.toolStripSeparator1,
            this.exitMenuItem});
            this.fileMenuItem.Name = "檔案ToolStripMenuItem";
            resources.ApplyResources(this.fileMenuItem, "檔案ToolStripMenuItem");
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "開啟ToolStripMenuItem";
            resources.ApplyResources(this.openMenuItem, "開啟ToolStripMenuItem");
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            // 
            // saveMenuItem
            // 
            this.saveMenuItem.Name = "存檔ToolStripMenuItem";
            resources.ApplyResources(this.saveMenuItem, "存檔ToolStripMenuItem");
            this.saveMenuItem.Click += new System.EventHandler(this.saveMenuItem_Click);
            // 
            // closeFileMenuItem
            // 
            this.closeFileMenuItem.Name = "關閉檔案CToolStripMenuItem";
            resources.ApplyResources(this.closeFileMenuItem, "關閉檔案CToolStripMenuItem");
            this.closeFileMenuItem.Click += new System.EventHandler(this.closeFileMenuItem_Click);
            // 
            // isRpcs3Format
            // 
            this.isRpcs3Format.Name = "isRpcs3Format";
            resources.ApplyResources(this.isRpcs3Format, "isRpcs3Format");
            this.isRpcs3Format.Click += new System.EventHandler(this.toggleRPCS3TrophyFormatToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "關閉ToolStripMenuItem";
            resources.ApplyResources(this.exitMenuItem, "關閉ToolStripMenuItem");
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // advancedMenuItem
            // 
            this.advancedMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.instantPlatinumMenuItem,
            this.clearTrophiesMenuItem,
            this.toolStripMenuItem1,
            this.toolStripSeparator2,
            this.setRandomStartTimeToolStripMenuItem,
            this.setRandomEndTimeToolStripMenuItem});
            resources.ApplyResources(this.advancedMenuItem, "進階ToolStripMenuItem");
            this.advancedMenuItem.Name = "進階ToolStripMenuItem";
            // 
            // instantPlatinumMenuItem
            // 
            this.instantPlatinumMenuItem.Name = "瞬間白金ToolStripMenuItem";
            resources.ApplyResources(this.instantPlatinumMenuItem, "瞬間白金ToolStripMenuItem");
            this.instantPlatinumMenuItem.Click += new System.EventHandler(this.instantPlatinumMenuItem_Click);
            // 
            // clearTrophiesMenuItem
            // 
            this.clearTrophiesMenuItem.Name = "清除獎杯ToolStripMenuItem";
            resources.ApplyResources(this.clearTrophiesMenuItem, "清除獎杯ToolStripMenuItem");
            this.clearTrophiesMenuItem.Click += new System.EventHandler(this.clearTrophiesMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // setRandomStartTimeToolStripMenuItem
            // 
            this.setRandomStartTimeToolStripMenuItem.Name = "setRandomStartTimeToolStripMenuItem";
            resources.ApplyResources(this.setRandomStartTimeToolStripMenuItem, "setRandomStartTimeToolStripMenuItem");
            this.setRandomStartTimeToolStripMenuItem.Click += new System.EventHandler(this.setRandomStartTimeToolStripMenuItem_Click);
            // 
            // setRandomEndTimeToolStripMenuItem
            // 
            this.setRandomEndTimeToolStripMenuItem.Name = "setRandomEndTimeToolStripMenuItem";
            resources.ApplyResources(this.setRandomEndTimeToolStripMenuItem, "setRandomEndTimeToolStripMenuItem");
            this.setRandomEndTimeToolStripMenuItem.Click += new System.EventHandler(this.setRandomEndTimeToolStripMenuItem_Click);
            // 
            // refreshMenuItem
            // 
            resources.ApplyResources(this.refreshMenuItem, "重新整理ToolStripMenuItem");
            this.refreshMenuItem.Name = "重新整理ToolStripMenuItem";
            this.refreshMenuItem.Click += new System.EventHandler(this.refreshMenuItem_Click);
            // 
            // toolStripComboBox2
            // 
            this.toolStripComboBox2.Name = "toolStripComboBox2";
            resources.ApplyResources(this.toolStripComboBox2, "toolStripComboBox2");
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.Items.AddRange(new object[] {
            resources.GetString("toolStripComboBox1.Items"),
            resources.GetString("toolStripComboBox1.Items1"),
            resources.GetString("toolStripComboBox1.Items2")});
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            resources.ApplyResources(this.toolStripComboBox1, "toolStripComboBox1");
            this.toolStripComboBox1.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBox1_SelectedIndexChanged);
            // 
            // dateTimePicker1
            // 
            resources.ApplyResources(this.dateTimePicker1, "dateTimePicker1");
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // listViewEx1
            // 
            this.listViewEx1.AllowColumnReorder = true;
            this.listViewEx1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader7,
            this.columnHeader6,
            this.columnHeader8,
            this.columnHeader9});
            resources.ApplyResources(this.listViewEx1, "listViewEx1");
            this.listViewEx1.DoubleClickActivation = false;
            this.listViewEx1.FullRowSelect = true;
            this.listViewEx1.HideSelection = false;
            this.listViewEx1.LargeImageList = this.imageList1;
            this.listViewEx1.Name = "listViewEx1";
            this.listViewEx1.SmallImageList = this.imageList1;
            this.listViewEx1.UseCompatibleStateImageBehavior = false;
            this.listViewEx1.View = System.Windows.Forms.View.Details;
            this.listViewEx1.SubItemClicked += new ListViewEx.SubItemEventHandler(this.listViewEx1_SubItemClicked);
            this.listViewEx1.SubItemEndEditing += new ListViewEx.SubItemEndEditingEventHandler(this.listViewEx1_SubItemEndEditing);
            this.listViewEx1.DoubleClick += new System.EventHandler(this.listViewEx1_DoubleClick);
            this.listViewEx1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewEx1_ColumnClick);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader5
            // 
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader7
            // 
            this.columnHeader7.Tag = "Sincronizado";
            resources.ApplyResources(this.columnHeader7, "columnHeader7");
            // 
            // columnHeader6
            // 
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // columnHeader8
            // 
            resources.ApplyResources(this.columnHeader8, "columnHeader8");
            //
            // columnHeader9
            //
            resources.ApplyResources(this.columnHeader9, "columnHeader9");
            //
            // MainAPP
            //
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.listViewEx1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainAPP";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel1;
        private ListViewEx.ListViewEx listViewEx1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.ToolStripMenuItem advancedMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolStripMenuItem refreshMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ToolStripMenuItem closeFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem instantPlatinumMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearTrophiesMenuItem;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.ToolStripMenuItem setRandomStartTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem setRandomEndTimeToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox2;
        private System.Windows.Forms.ToolStripMenuItem isRpcs3Format;
    }
}

