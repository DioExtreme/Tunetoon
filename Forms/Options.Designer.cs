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
            this.ClashDistrictCheckBox = new System.Windows.Forms.CheckBox();
            this.DistrictComboBox = new System.Windows.Forms.ComboBox();
            this.EncryptAccsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // OkayButton
            // 
            this.OkayButton.Location = new System.Drawing.Point(12, 198);
            this.OkayButton.Name = "OkayButton";
            this.OkayButton.Size = new System.Drawing.Size(483, 23);
            this.OkayButton.TabIndex = 0;
            this.OkayButton.Text = "Save";
            this.OkayButton.UseVisualStyleBackColor = true;
            this.OkayButton.Click += new System.EventHandler(this.OkayButton_Click);
            // 
            // RewrittenLabel
            // 
            this.RewrittenLabel.AutoSize = true;
            this.RewrittenLabel.Location = new System.Drawing.Point(9, 14);
            this.RewrittenLabel.Name = "RewrittenLabel";
            this.RewrittenLabel.Size = new System.Drawing.Size(80, 13);
            this.RewrittenLabel.TabIndex = 3;
            this.RewrittenLabel.Text = "Rewritten Path:";
            // 
            // ClashLabel
            // 
            this.ClashLabel.AutoSize = true;
            this.ClashLabel.Location = new System.Drawing.Point(9, 64);
            this.ClashLabel.Name = "ClashLabel";
            this.ClashLabel.Size = new System.Drawing.Size(61, 13);
            this.ClashLabel.TabIndex = 4;
            this.ClashLabel.Text = "Clash Path:";
            // 
            // SelectionCheckBox
            // 
            this.SelectionCheckBox.AutoSize = true;
            this.SelectionCheckBox.Location = new System.Drawing.Point(341, 118);
            this.SelectionCheckBox.Name = "SelectionCheckBox";
            this.SelectionCheckBox.Size = new System.Drawing.Size(104, 17);
            this.SelectionCheckBox.TabIndex = 5;
            this.SelectionCheckBox.Text = "End by selection";
            this.SelectionCheckBox.UseVisualStyleBackColor = true;
            // 
            // GlobalEndCheckBox
            // 
            this.GlobalEndCheckBox.AutoSize = true;
            this.GlobalEndCheckBox.Location = new System.Drawing.Point(341, 141);
            this.GlobalEndCheckBox.Name = "GlobalEndCheckBox";
            this.GlobalEndCheckBox.Size = new System.Drawing.Size(160, 17);
            this.GlobalEndCheckBox.TabIndex = 6;
            this.GlobalEndCheckBox.Text = "\"End All\" for all gameservers";
            this.GlobalEndCheckBox.UseVisualStyleBackColor = true;
            // 
            // RewrittenPath
            // 
            this.RewrittenPath.Location = new System.Drawing.Point(12, 30);
            this.RewrittenPath.Name = "RewrittenPath";
            this.RewrittenPath.Size = new System.Drawing.Size(444, 20);
            this.RewrittenPath.TabIndex = 9;
            // 
            // ClashPath
            // 
            this.ClashPath.Location = new System.Drawing.Point(12, 80);
            this.ClashPath.Name = "ClashPath";
            this.ClashPath.Size = new System.Drawing.Size(444, 20);
            this.ClashPath.TabIndex = 10;
            // 
            // RewrittenPathButton
            // 
            this.RewrittenPathButton.Location = new System.Drawing.Point(462, 30);
            this.RewrittenPathButton.Name = "RewrittenPathButton";
            this.RewrittenPathButton.Size = new System.Drawing.Size(33, 20);
            this.RewrittenPathButton.TabIndex = 11;
            this.RewrittenPathButton.Text = "...";
            this.RewrittenPathButton.UseVisualStyleBackColor = true;
            this.RewrittenPathButton.Click += new System.EventHandler(this.RewrittenPathButton_Click);
            // 
            // ClashPathButton
            // 
            this.ClashPathButton.Location = new System.Drawing.Point(462, 80);
            this.ClashPathButton.Name = "ClashPathButton";
            this.ClashPathButton.Size = new System.Drawing.Size(33, 21);
            this.ClashPathButton.TabIndex = 12;
            this.ClashPathButton.Text = "...";
            this.ClashPathButton.UseVisualStyleBackColor = true;
            this.ClashPathButton.Click += new System.EventHandler(this.ClashPathButton_Click);
            // 
            // SkipUpdatesCheckBox
            // 
            this.SkipUpdatesCheckBox.AutoSize = true;
            this.SkipUpdatesCheckBox.Location = new System.Drawing.Point(15, 118);
            this.SkipUpdatesCheckBox.Name = "SkipUpdatesCheckBox";
            this.SkipUpdatesCheckBox.Size = new System.Drawing.Size(117, 17);
            this.SkipUpdatesCheckBox.TabIndex = 14;
            this.SkipUpdatesCheckBox.Text = "Skip game updates";
            this.SkipUpdatesCheckBox.UseVisualStyleBackColor = true;
            // 
            // ClashDistrictCheckBox
            // 
            this.ClashDistrictCheckBox.AutoSize = true;
            this.ClashDistrictCheckBox.Location = new System.Drawing.Point(15, 141);
            this.ClashDistrictCheckBox.Name = "ClashDistrictCheckBox";
            this.ClashDistrictCheckBox.Size = new System.Drawing.Size(172, 17);
            this.ClashDistrictCheckBox.TabIndex = 15;
            this.ClashDistrictCheckBox.Text = "(Clash) Start all toons in district:";
            this.ClashDistrictCheckBox.UseVisualStyleBackColor = true;
            this.ClashDistrictCheckBox.CheckedChanged += new System.EventHandler(this.ClashDistrictCheckBox_CheckedChanged);
            // 
            // DistrictComboBox
            // 
            this.DistrictComboBox.FormattingEnabled = true;
            this.DistrictComboBox.Location = new System.Drawing.Point(15, 164);
            this.DistrictComboBox.Name = "DistrictComboBox";
            this.DistrictComboBox.Size = new System.Drawing.Size(160, 21);
            this.DistrictComboBox.TabIndex = 16;
            // 
            // EncryptAccsCheckBox
            // 
            this.EncryptAccsCheckBox.AutoSize = true;
            this.EncryptAccsCheckBox.Location = new System.Drawing.Point(341, 164);
            this.EncryptAccsCheckBox.Name = "EncryptAccsCheckBox";
            this.EncryptAccsCheckBox.Size = new System.Drawing.Size(109, 17);
            this.EncryptAccsCheckBox.TabIndex = 17;
            this.EncryptAccsCheckBox.Text = "Encrypt accounts";
            this.EncryptAccsCheckBox.UseVisualStyleBackColor = true;
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 228);
            this.Controls.Add(this.EncryptAccsCheckBox);
            this.Controls.Add(this.DistrictComboBox);
            this.Controls.Add(this.ClashDistrictCheckBox);
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
        private System.Windows.Forms.CheckBox ClashDistrictCheckBox;
        private System.Windows.Forms.ComboBox DistrictComboBox;
        private System.Windows.Forms.CheckBox EncryptAccsCheckBox;
    }
}