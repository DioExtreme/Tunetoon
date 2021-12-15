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

        private List<string> clashDistricts = new List<string>();

        public Options(Launcher launcherWnd, Config config)
        {
            this.launcherWnd = launcherWnd;
            this.config = config;

            clashDistricts.Add("Anvil Acres");
            clashDistricts.Add("Cupcake Cove");
            clashDistricts.Add("High-Dive Hills");
            clashDistricts.Add("Hypno Heights");
            clashDistricts.Add("Kazoo Kanyon");
            clashDistricts.Add("Quicksand Quarry");
            clashDistricts.Add("Seltzer Summit");
            clashDistricts.Add("Tesla Tundra");

            InitializeComponent();

            DistrictComboBox.DataSource = clashDistricts;
        }

        private void Options_Load(object sender, EventArgs e)
        {
            RewrittenPath.Text = config.RewrittenPath;
            ClashPath.Text = config.ClashPath;
            MultitoonPath.Text = config.MultiPath;

            SkipUpdatesCheckBox.Checked = config.SkipUpdates;
            SelectionCheckBox.Checked = endSelectionChecked = config.SelectEndGames;
            GlobalEndCheckBox.Checked = config.GlobalEndAll;
            EncryptAccsCheckBox.Checked = config.EncryptAccounts;
            // Fixes annoying oversight with clash district not being loaded in options menu
            DistrictComboBox.SelectedIndex = clashDistricts.IndexOf(config.ClashDistrict);
            LaunchMultitoonCheckBox.Checked = config.LaunchMultitoonWhenPlay;

            ClashDistrictCheckBox.Checked = DistrictComboBox.Enabled = config.ClashDistrict != null;
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
            config.MultiPath = MultitoonPath.Text;

            config.SkipUpdates = SkipUpdatesCheckBox.Checked;
            config.SelectEndGames = SelectionCheckBox.Checked;
            config.GlobalEndAll = GlobalEndCheckBox.Checked;
            config.EncryptAccounts = EncryptAccsCheckBox.Checked;
            config.LaunchMultitoonWhenPlay = LaunchMultitoonCheckBox.Checked;

            config.ClashDistrict = ClashDistrictCheckBox.Checked ? DistrictComboBox.SelectedItem.ToString() : null;

            if (endSelectionChecked != config.SelectEndGames)
            {
                launcherWnd.SelectionOptionAltered();
            }

            Dispose();
        }

        private void ClashDistrictCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DistrictComboBox.Enabled = ClashDistrictCheckBox.Checked;
        }

        private void MultitoonPathButton_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Executable files (*.exe)|*.exe|All files|*";
            ofd.Title = "Select multitoon executable";
            ofd.ShowDialog();

            if (ofd.FileName != string.Empty)
            {
                MultitoonPath.Text = ofd.FileName;
                MultitoonPath.ReadOnly = true;
            }
        }
    }
}
