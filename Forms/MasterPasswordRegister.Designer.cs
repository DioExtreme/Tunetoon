namespace Tunetoon.Forms
{
    partial class MasterPasswordRegister
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MasterPasswordRegister));
            this.protectButton = new System.Windows.Forms.Button();
            this.passwordTextbox = new System.Windows.Forms.TextBox();
            this.passwordConfirmTextbox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.confirmLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // protectButton
            // 
            this.protectButton.Location = new System.Drawing.Point(9, 88);
            this.protectButton.Name = "protectButton";
            this.protectButton.Size = new System.Drawing.Size(265, 24);
            this.protectButton.TabIndex = 0;
            this.protectButton.Text = "Protect";
            this.protectButton.UseVisualStyleBackColor = true;
            this.protectButton.Click += new System.EventHandler(this.protectButton_Click);
            // 
            // passwordTextbox
            // 
            this.passwordTextbox.Location = new System.Drawing.Point(82, 22);
            this.passwordTextbox.Name = "passwordTextbox";
            this.passwordTextbox.Size = new System.Drawing.Size(192, 23);
            this.passwordTextbox.TabIndex = 1;
            this.passwordTextbox.UseSystemPasswordChar = true;
            this.passwordTextbox.TextChanged += new System.EventHandler(this.passwordTextbox_TextChanged);
            // 
            // passwordConfirmTextbox
            // 
            this.passwordConfirmTextbox.Location = new System.Drawing.Point(82, 59);
            this.passwordConfirmTextbox.Name = "passwordConfirmTextbox";
            this.passwordConfirmTextbox.Size = new System.Drawing.Size(192, 23);
            this.passwordConfirmTextbox.TabIndex = 2;
            this.passwordConfirmTextbox.UseSystemPasswordChar = true;
            this.passwordConfirmTextbox.TextChanged += new System.EventHandler(this.passwordConfirmTextbox_TextChanged);
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Location = new System.Drawing.Point(9, 25);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(60, 15);
            this.passwordLabel.TabIndex = 3;
            this.passwordLabel.Text = "Password:";
            // 
            // confirmLabel
            // 
            this.confirmLabel.AutoSize = true;
            this.confirmLabel.Location = new System.Drawing.Point(9, 62);
            this.confirmLabel.Name = "confirmLabel";
            this.confirmLabel.Size = new System.Drawing.Size(54, 15);
            this.confirmLabel.TabIndex = 4;
            this.confirmLabel.Text = "Confirm:";
            // 
            // MasterPasswordRegister
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(283, 119);
            this.Controls.Add(this.confirmLabel);
            this.Controls.Add(this.passwordLabel);
            this.Controls.Add(this.passwordConfirmTextbox);
            this.Controls.Add(this.passwordTextbox);
            this.Controls.Add(this.protectButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MasterPasswordRegister";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Master Password";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onFormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button protectButton;
        private System.Windows.Forms.TextBox passwordTextbox;
        private System.Windows.Forms.TextBox passwordConfirmTextbox;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.Label confirmLabel;
    }
}