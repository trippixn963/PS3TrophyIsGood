namespace PS3TrophyIsGood
{
    partial class CopyFrom
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
            this.urlLabel = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.accept = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // urlLabel
            //
            this.urlLabel.AutoSize = true;
            this.urlLabel.Location = new System.Drawing.Point(12, 14);
            this.urlLabel.Name = "urlLabel";
            this.urlLabel.Text =
                "Paste the donor's PSNProfiles game-trophy URL:\r\n"
                + "e.g.  https://psnprofiles.com/trophies/1-super-stardust-hd/SomeUser";
            //
            // textBox1
            //
            this.textBox1.Location = new System.Drawing.Point(13, 52);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(459, 23);
            //
            // accept
            //
            this.accept.Location = new System.Drawing.Point(316, 86);
            this.accept.Name = "accept";
            this.accept.Size = new System.Drawing.Size(75, 27);
            this.accept.Text = "Import";
            this.accept.UseVisualStyleBackColor = true;
            this.accept.Click += new System.EventHandler(this.accept_Click);
            //
            // button2
            //
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(397, 86);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 27);
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            //
            // CopyFrom
            //
            this.AcceptButton = this.accept;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.ClientSize = new System.Drawing.Size(484, 125);
            this.Controls.Add(this.urlLabel);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.accept);
            this.Controls.Add(this.textBox1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "CopyFrom";
            this.Text = "Copy from PSNProfiles";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label urlLabel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button accept;
        private System.Windows.Forms.Button button2;
    }
}
