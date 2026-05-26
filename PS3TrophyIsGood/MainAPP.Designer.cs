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
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.advancedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearTrophiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripComboBox2 = new System.Windows.Forms.ToolStripComboBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
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
            this.toolStripComboBox2});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMenuItem,
            this.saveMenuItem,
            this.closeFileMenuItem,
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
            this.clearTrophiesMenuItem,
            this.toolStripMenuItem1});
            resources.ApplyResources(this.advancedMenuItem, "進階ToolStripMenuItem");
            this.advancedMenuItem.Name = "進階ToolStripMenuItem";
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
            // dateTimePicker1
            // 
            resources.ApplyResources(this.dateTimePicker1, "dateTimePicker1");
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.TabStop = false;
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
        private System.Windows.Forms.ToolStripMenuItem refreshMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ToolStripMenuItem closeFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearTrophiesMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox2;
    }
}

