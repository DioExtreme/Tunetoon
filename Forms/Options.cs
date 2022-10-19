using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tunetoon.Forms
{
    public partial class Options : Form
    {
        private Launcher launcherWnd;
        private Config config;
        private bool endSelectionChecked;

        public Options(Launcher launcherWnd, Config config)
        {
            this.launcherWnd = launcherWnd;
            this.config = config;

            InitializeComponent();
        }

        private void Options_Load(object sender, EventArgs e)
        {
            RewrittenPath.Text = config.RewrittenPath;
            ClashPath.Text = config.ClashPath;

            SkipUpdatesCheckBox.Checked = config.SkipUpdates;
            SelectionCheckBox.Checked = endSelectionChecked = config.SelectEndGames;
            GlobalEndCheckBox.Checked = config.GlobalEndAll;
            EncryptAccsCheckBox.Checked = config.EncryptAccounts;
        }

        private void RewrittenPathButton_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();

            fbd.Description = "Select Toontown Rewritten's folder.";

            fbd.ShowDialog();
            if (fbd.SelectedPath != string.Empty)
            {
                RewrittenPath.Text = fbd.SelectedPath + "\\";
                RewrittenPath.ReadOnly = true;
            }
        }

        private void ClashPathButton_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.Description = "Select Corporate Clash's folder.";
            fbd.ShowDialog();

            if (fbd.SelectedPath != string.Empty)
            {
                ClashPath.Text = fbd.SelectedPath + "\\";
                ClashPath.ReadOnly = true;
            }
        }

        private void OkayButton_Click(object sender, EventArgs e)
        {
            config.RewrittenPath = RewrittenPath.Text;
            config.ClashPath = ClashPath.Text;

            config.SkipUpdates = SkipUpdatesCheckBox.Checked;
            config.SelectEndGames = SelectionCheckBox.Checked;
            config.GlobalEndAll = GlobalEndCheckBox.Checked;
            config.EncryptAccounts = EncryptAccsCheckBox.Checked;
 
            if (endSelectionChecked != config.SelectEndGames)
            {
                launcherWnd.SelectionOptionAltered();
            }

            Dispose();
        }
    }
}
