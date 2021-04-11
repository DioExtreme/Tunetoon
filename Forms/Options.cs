using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tunetoon.Forms
{
    public partial class Options : Form
    {
        private Launcher LauncherWnd;
        private Config Config;
        private bool endSelectionChecked;

        private List<string> clashDistricts = new List<string>();

        public Options(Launcher LauncherWnd, Config Config)
        {
            this.LauncherWnd = LauncherWnd;
            this.Config = Config;

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
            RewrittenPath.Text = Config.RewrittenPath;
            ClashPath.Text = Config.ClashPath;

            SkipUpdatesCheckBox.Checked = Config.SkipUpdates;
            SelectionCheckBox.Checked = endSelectionChecked = Config.SelectEndGames;
            GlobalEndCheckBox.Checked = Config.GlobalEndAll;
            ClashDistrictCheckBox.Checked = DistrictComboBox.Enabled = Config.ClashDistrict != null;
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
            Config.RewrittenPath = RewrittenPath.Text;
            Config.ClashPath = ClashPath.Text;
            Config.SkipUpdates = SkipUpdatesCheckBox.Checked;
            Config.SelectEndGames = SelectionCheckBox.Checked;
            Config.GlobalEndAll = GlobalEndCheckBox.Checked;

            Config.ClashDistrict = ClashDistrictCheckBox.Checked ? DistrictComboBox.SelectedItem.ToString() : null;
 
            if (endSelectionChecked != Config.SelectEndGames)
            {
                LauncherWnd.SelectionOptionAltered();
            }

            Dispose();
        }

        private void Help_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A Discord handle would be here but it's removed for the open source release.",
                "Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClashDistrictCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DistrictComboBox.Enabled = ClashDistrictCheckBox.Checked;
        }
    }
}
