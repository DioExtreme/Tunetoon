namespace Tunetoon.Forms
{
    partial class MasterPasswordEnter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MasterPasswordEnter));
            this.startButton = new System.Windows.Forms.Button();
            this.passwordTextbox = new System.Windows.Forms.TextBox();
            this.enterPasswordLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(9, 56);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(262, 23);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // passwordTextbox
            // 
            this.passwordTextbox.Location = new System.Drawing.Point(9, 27);
            this.passwordTextbox.Name = "passwordTextbox";
            this.passwordTextbox.Size = new System.Drawing.Size(260, 23);
            this.passwordTextbox.TabIndex = 1;
            this.passwordTextbox.UseSystemPasswordChar = true;
            // 
            // enterPasswordLabel
            // 
            this.enterPasswordLabel.AutoSize = true;
            this.enterPasswordLabel.Location = new System.Drawing.Point(9, 9);
            this.enterPasswordLabel.Name = "enterPasswordLabel";
            this.enterPasswordLabel.Size = new System.Drawing.Size(129, 15);
            this.enterPasswordLabel.TabIndex = 2;
            this.enterPasswordLabel.Text = "Enter master password:";
            // 
            // MasterPasswordEnter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(283, 85);
            this.Controls.Add(this.enterPasswordLabel);
            this.Controls.Add(this.passwordTextbox);
            this.Controls.Add(this.startButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MasterPasswordEnter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Master Password";
            this.Load += new System.EventHandler(this.MasterPasswordEnter_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.TextBox passwordTextbox;
        private System.Windows.Forms.Label enterPasswordLabel;
    }
}