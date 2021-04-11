namespace Tunetoon.Forms
{
    partial class Auth
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Auth));
            this.responseLabel = new System.Windows.Forms.Label();
            this.Auth2Button = new System.Windows.Forms.Button();
            this.AuthGrid = new System.Windows.Forms.DataGridView();
            this.Cancel = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Toon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AuthToken = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.AuthGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // responseLabel
            // 
            this.responseLabel.AutoSize = true;
            this.responseLabel.Location = new System.Drawing.Point(8, 9);
            this.responseLabel.Name = "responseLabel";
            this.responseLabel.Size = new System.Drawing.Size(255, 13);
            this.responseLabel.TabIndex = 0;
            this.responseLabel.Text = "The following toons require additional authentication:";
            // 
            // Auth2Button
            // 
            this.Auth2Button.Location = new System.Drawing.Point(11, 241);
            this.Auth2Button.Name = "Auth2Button";
            this.Auth2Button.Size = new System.Drawing.Size(314, 23);
            this.Auth2Button.TabIndex = 2;
            this.Auth2Button.Text = "Auth";
            this.Auth2Button.UseVisualStyleBackColor = true;
            this.Auth2Button.Click += new System.EventHandler(this.Auth2Button_Click);
            // 
            // AuthGrid
            // 
            this.AuthGrid.AllowUserToAddRows = false;
            this.AuthGrid.AllowUserToDeleteRows = false;
            this.AuthGrid.AllowUserToResizeColumns = false;
            this.AuthGrid.AllowUserToResizeRows = false;
            this.AuthGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.AuthGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.AuthGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Cancel,
            this.Toon,
            this.AuthToken});
            this.AuthGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.AuthGrid.Location = new System.Drawing.Point(11, 25);
            this.AuthGrid.Name = "AuthGrid";
            this.AuthGrid.RowHeadersVisible = false;
            this.AuthGrid.Size = new System.Drawing.Size(314, 210);
            this.AuthGrid.TabIndex = 3;
            this.AuthGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.AuthGrid_CellContentClick);
            this.AuthGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.AuthGrid_CellValueChanged);
            this.AuthGrid.MouseClick += new System.Windows.Forms.MouseEventHandler(this.AuthGrid_MouseClick);
            // 
            // Cancel
            // 
            this.Cancel.DataPropertyName = "cancelled";
            this.Cancel.HeaderText = "Cancel?";
            this.Cancel.Name = "Cancel";
            this.Cancel.Width = 60;
            // 
            // Toon
            // 
            this.Toon.DataPropertyName = "Toon";
            this.Toon.HeaderText = "Toon";
            this.Toon.Name = "Toon";
            this.Toon.ReadOnly = true;
            this.Toon.Width = 115;
            // 
            // AuthToken
            // 
            this.AuthToken.DataPropertyName = "AuthToken";
            this.AuthToken.HeaderText = "Auth Token";
            this.AuthToken.Name = "AuthToken";
            this.AuthToken.Width = 115;
            // 
            // Auth
            // 
            this.AcceptButton = this.Auth2Button;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 268);
            this.Controls.Add(this.AuthGrid);
            this.Controls.Add(this.Auth2Button);
            this.Controls.Add(this.responseLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Auth";
            this.Text = "Auth";
            this.Load += new System.EventHandler(this.Auth_Load);
            ((System.ComponentModel.ISupportInitialize)(this.AuthGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label responseLabel;
        private System.Windows.Forms.Button Auth2Button;
        private System.Windows.Forms.DataGridView AuthGrid;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Cancel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Toon;
        private System.Windows.Forms.DataGridViewTextBoxColumn AuthToken;
    }
}