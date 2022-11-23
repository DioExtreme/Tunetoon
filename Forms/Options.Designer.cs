namespace Tunetoon.Forms
{
    partial class Options
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.OkayButton = new System.Windows.Forms.Button();
            this.RewrittenLabel = new System.Windows.Forms.Label();
            this.ClashLabel = new System.Windows.Forms.Label();
            this.SelectionCheckBox = new System.Windows.Forms.CheckBox();
            this.GlobalEndCheckBox = new System.Windows.Forms.CheckBox();
            this.RewrittenPath = new System.Windows.Forms.TextBox();
            this.ClashPath = new System.Windows.Forms.TextBox();
            this.RewrittenPathButton = new System.Windows.Forms.Button();
            this.ClashPathButton = new System.Windows.Forms.Button();
            this.SkipUpdatesCheckBox = new System.Windows.Forms.CheckBox();
            this.EncryptAccsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // OkayButton
            // 
            this.OkayButton.Location = new System.Drawing.Point(14, 201);
            this.OkayButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.OkayButton.Name = "OkayButton";
            this.OkayButton.Size = new System.Drawing.Size(564, 27);
            this.OkayButton.TabIndex = 0;
            this.OkayButton.Text = "Save";
            this.OkayButton.UseVisualStyleBackColor = true;
            this.OkayButton.Click += new System.EventHandler(this.OkayButton_Click);
            // 
            // RewrittenLabel
            // 
            this.RewrittenLabel.AutoSize = true;
            this.RewrittenLabel.Location = new System.Drawing.Point(10, 16);
            this.RewrittenLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.RewrittenLabel.Name = "RewrittenLabel";
            this.RewrittenLabel.Size = new System.Drawing.Size(87, 15);
            this.RewrittenLabel.TabIndex = 3;
            this.RewrittenLabel.Text = "Rewritten Path:";
            // 
            // ClashLabel
            // 
            this.ClashLabel.AutoSize = true;
            this.ClashLabel.Location = new System.Drawing.Point(10, 74);
            this.ClashLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ClashLabel.Name = "ClashLabel";
            this.ClashLabel.Size = new System.Drawing.Size(66, 15);
            this.ClashLabel.TabIndex = 4;
            this.ClashLabel.Text = "Clash Path:";
            // 
            // SelectionCheckBox
            // 
            this.SelectionCheckBox.AutoSize = true;
            this.SelectionCheckBox.Location = new System.Drawing.Point(398, 136);
            this.SelectionCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.SelectionCheckBox.Name = "SelectionCheckBox";
            this.SelectionCheckBox.Size = new System.Drawing.Size(112, 19);
            this.SelectionCheckBox.TabIndex = 5;
            this.SelectionCheckBox.Text = "End by selection";
            this.SelectionCheckBox.UseVisualStyleBackColor = true;
            // 
            // GlobalEndCheckBox
            // 
            this.GlobalEndCheckBox.AutoSize = true;
            this.GlobalEndCheckBox.Location = new System.Drawing.Point(398, 163);
            this.GlobalEndCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.GlobalEndCheckBox.Name = "GlobalEndCheckBox";
            this.GlobalEndCheckBox.Size = new System.Drawing.Size(175, 19);
            this.GlobalEndCheckBox.TabIndex = 6;
            this.GlobalEndCheckBox.Text = "\"End All\" for all gameservers";
            this.GlobalEndCheckBox.UseVisualStyleBackColor = true;
            // 
            // RewrittenPath
            // 
            this.RewrittenPath.Location = new System.Drawing.Point(14, 35);
            this.RewrittenPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.RewrittenPath.Name = "RewrittenPath";
            this.RewrittenPath.Size = new System.Drawing.Size(517, 23);
            this.RewrittenPath.TabIndex = 9;
            // 
            // ClashPath
            // 
            this.ClashPath.Location = new System.Drawing.Point(14, 92);
            this.ClashPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ClashPath.Name = "ClashPath";
            this.ClashPath.Size = new System.Drawing.Size(517, 23);
            this.ClashPath.TabIndex = 10;
            // 
            // RewrittenPathButton
            // 
            this.RewrittenPathButton.Location = new System.Drawing.Point(539, 35);
            this.RewrittenPathButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.RewrittenPathButton.Name = "RewrittenPathButton";
            this.RewrittenPathButton.Size = new System.Drawing.Size(38, 23);
            this.RewrittenPathButton.TabIndex = 11;
            this.RewrittenPathButton.Text = "...";
            this.RewrittenPathButton.UseVisualStyleBackColor = true;
            this.RewrittenPathButton.Click += new System.EventHandler(this.RewrittenPathButton_Click);
            // 
            // ClashPathButton
            // 
            this.ClashPathButton.Location = new System.Drawing.Point(539, 92);
            this.ClashPathButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ClashPathButton.Name = "ClashPathButton";
            this.ClashPathButton.Size = new System.Drawing.Size(38, 24);
            this.ClashPathButton.TabIndex = 12;
            this.ClashPathButton.Text = "...";
            this.ClashPathButton.UseVisualStyleBackColor = true;
            this.ClashPathButton.Click += new System.EventHandler(this.ClashPathButton_Click);
            // 
            // SkipUpdatesCheckBox
            // 
            this.SkipUpdatesCheckBox.AutoSize = true;
            this.SkipUpdatesCheckBox.Location = new System.Drawing.Point(18, 136);
            this.SkipUpdatesCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.SkipUpdatesCheckBox.Name = "SkipUpdatesCheckBox";
            this.SkipUpdatesCheckBox.Size = new System.Drawing.Size(126, 19);
            this.SkipUpdatesCheckBox.TabIndex = 14;
            this.SkipUpdatesCheckBox.Text = "Skip game updates";
            this.SkipUpdatesCheckBox.UseVisualStyleBackColor = true;
            // 
            // EncryptAccsCheckBox
            // 
            this.EncryptAccsCheckBox.AutoSize = true;
            this.EncryptAccsCheckBox.Location = new System.Drawing.Point(18, 163);
            this.EncryptAccsCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.EncryptAccsCheckBox.Name = "EncryptAccsCheckBox";
            this.EncryptAccsCheckBox.Size = new System.Drawing.Size(117, 19);
            this.EncryptAccsCheckBox.TabIndex = 17;
            this.EncryptAccsCheckBox.Text = "Encrypt accounts";
            this.EncryptAccsCheckBox.UseVisualStyleBackColor = true;
            this.EncryptAccsCheckBox.CheckedChanged += new System.EventHandler(this.EncryptAccsCheckBox_CheckedChanged);
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(593, 234);
            this.Controls.Add(this.EncryptAccsCheckBox);
            this.Controls.Add(this.SkipUpdatesCheckBox);
            this.Controls.Add(this.ClashPathButton);
            this.Controls.Add(this.RewrittenPathButton);
            this.Controls.Add(this.ClashPath);
            this.Controls.Add(this.RewrittenPath);
            this.Controls.Add(this.GlobalEndCheckBox);
            this.Controls.Add(this.SelectionCheckBox);
            this.Controls.Add(this.ClashLabel);
            this.Controls.Add(this.RewrittenLabel);
            this.Controls.Add(this.OkayButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Options";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.Options_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OkayButton;
        private System.Windows.Forms.Label RewrittenLabel;
        private System.Windows.Forms.Label ClashLabel;
        private System.Windows.Forms.CheckBox SelectionCheckBox;
        private System.Windows.Forms.CheckBox GlobalEndCheckBox;
        private System.Windows.Forms.TextBox RewrittenPath;
        private System.Windows.Forms.TextBox ClashPath;
        private System.Windows.Forms.Button RewrittenPathButton;
        private System.Windows.Forms.Button ClashPathButton;
        private System.Windows.Forms.CheckBox SkipUpdatesCheckBox;
        private System.Windows.Forms.CheckBox EncryptAccsCheckBox;
    }
}