using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tunetoon.Accounts;
using Tunetoon.Game;
using Tunetoon.Grid;
using Tunetoon.Login;
using Tunetoon.Patcher;
using Tunetoon.Utilities;

namespace Tunetoon.Forms
{
    public partial class Launcher : Form
    {
        private DataHandler dataHandler = new DataHandler();

        private AccountList<RewrittenAccount> rewrittenAccountList = new AccountList<RewrittenAccount>();
        private AccountList<ClashAccount> clashAccountList = new AccountList<ClashAccount>();
        private dynamic currentAccountList;

        private BindingSource bindingSource = new BindingSource();

        // Patchers
        private RewrittenPatcher rewrittenPatcher;
        private ClashPatcher clashPatcher;
        private IPatcher gamePatcher;

        // Login handlers
        private RewrittenLoginHandler rewrittenLoginHandler = new RewrittenLoginHandler();
        private ClashLoginHandler clashLoginHandler = new ClashLoginHandler();
        private dynamic loginHandler;

        // Game handlers
        private RewrittenGameHandler rewrittenGameHandler;
        private ClashGameHandler clashGameHandler;
        private dynamic gameHandler;

        // UI handlers
        private RewrittenGridHandler rewrittenGridHandler = new RewrittenGridHandler();
        private ClashGridHandler clashGridHandler = new ClashGridHandler();
        private IGridHandler gridHandler;

        private Config config;

        public Launcher()
        {
            bindingSource.ListChanged += BindingSource_ListChanged;

            DataProtection.LoadEntropy();
            config = dataHandler.LoadConfig("Config.json");

            rewrittenPatcher = new RewrittenPatcher(config);
            clashPatcher = new ClashPatcher(config);
            rewrittenGameHandler = new RewrittenGameHandler(config);
            clashGameHandler = new ClashGameHandler(config);

            dataHandler.LoadClashIngameToons(config);
            dataHandler.LoadAccounts(ref rewrittenAccountList, "AccListRewritten.nully");
            dataHandler.LoadAccounts(ref clashAccountList, "AccListClash.nully");

            InitializeComponent();

            HandleConfig();

            accountGrid.AutoGenerateColumns = false;
            bindingSource.DataSource = currentAccountList;
            accountGrid.DataSource = bindingSource;
        }

        private void HandleConfig()
        {
            if (config.GameServer == Server.REWRITTEN)
            {
                currentAccountList = rewrittenAccountList;
                gamePatcher = rewrittenPatcher;
                loginHandler = rewrittenLoginHandler;
                gameHandler = rewrittenGameHandler;
                gridHandler = rewrittenGridHandler;
                rewrittenMenuItem.Checked = true;
                clashMenuItem.Checked = false;
                accountGrid.Columns[ToonSlots.Index].Visible = false;
            }
            else
            {
                currentAccountList = clashAccountList;
                gamePatcher = clashPatcher;
                loginHandler = clashLoginHandler;
                gameHandler = clashGameHandler;
                gridHandler = clashGridHandler;
                rewrittenMenuItem.Checked = false;
                clashMenuItem.Checked = true;
                accountGrid.Columns[ToonSlots.Index].Visible = true;
            }

            if (config.SelectEndGames)
            {
                endSelectedMenuItem.Visible = true;
                accountGrid.Columns[End.Index].ReadOnly = false;
            }
        }

        private void ShowPatcherError(string text)
        {
            MessageBox.Show(text, "Game patcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task RunPatcherAsync()
        {
            await Task.Run(() => gamePatcher.GetPatchManifest());

            if (gamePatcher.HasFailed())
            {
                ShowPatcherError("Could not retrieve patch manifest. The game has not been updated.");
                return;
            }

            string actionStr = "Checking";

            var progress = new Progress<PatchProgress>(p =>
            {
                Text = "Tunetoon - " + actionStr + " Files " + p.CurrentFilesProcessed + "/" + p.TotalFilesToProcess;
            });

            await Task.Run(() => gamePatcher.CheckGameFiles(progress));

            actionStr = "Downloading";
            await Task.Run(() => gamePatcher.DownloadGameFiles(progress));

            if (gamePatcher.HasFailed())
            {
                ShowPatcherError("An error occured downloading update files. The game has not been updated.");
                return;
            }

            actionStr = "Patching";
            await Task.Run(() => gamePatcher.PatchGameFiles(progress));

            if (gamePatcher.HasFailed())
            {
                ShowPatcherError("An error occured applying game patches. The game has not been updated.");
            }
        }

        public async Task StartUpdate()
        {
            // Don't update if user does not want to or the game is running
            if (config.SkipUpdates || gameHandler.ActiveProcesses.Count > 0)
            {
                return;
            }

            try
            {
                if (config.GameServer == Server.REWRITTEN && !Directory.Exists(config.RewrittenPath))
                {
                    Directory.CreateDirectory(config.RewrittenPath);
                }
                else if (config.GameServer == Server.CLASH && !Directory.Exists(config.ClashPath))
                {
                    Directory.CreateDirectory(config.ClashPath);
                }
            }
            catch
            {
                return;
            }

            LoginButton.Enabled = false;
            serverMenuItem.Enabled = false;
            LoginButton.Text = "Checking for updates...";
            
            await RunPatcherAsync();

            Text = "Tunetoon";
            LoginButton.Text = "Play";
            LoginButton.Enabled = true;
            serverMenuItem.Enabled = true;
        }

        private async void Launcher_Load(object sender, EventArgs e)
        {
            await StartUpdate();
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            // Workarounds some ComboBoxCell glitch
            // where the selection is not saved
            accountGrid.ClearGridSelections();

            LoginButton.Enabled = false;

            if (config.GameServer == Server.REWRITTEN && !Directory.Exists(config.RewrittenPath) ||
                config.GameServer == Server.CLASH && !Directory.Exists(config.ClashPath))
            {
                MessageBox.Show("Game directory missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                await LoginAccounts();
            }
            catch
            {
                MessageBox.Show("An error occured during the login process.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            LoginButton.Enabled = true;
        }


        // Used by Rewritten only currently
        // Opens a form where the user can input two-step tokens
        private async Task HandleTwoStep()
        {
            var accountsToTwoStepAuth = loginHandler.AccountsToTwoStepAuth;
            while (accountsToTwoStepAuth.Count > 0)
            {
                bool authCancelled = false;
                var authForm = new Auth(accountsToTwoStepAuth);
                authForm.AuthTokensEntered += (accountsToAuth) => accountsToTwoStepAuth = accountsToAuth;
                authForm.IsClosed += () => authCancelled = true;
                authForm.ShowDialog();

                if (authCancelled)
                {
                    break;
                }

                await rewrittenLoginHandler.HandleTwoStep();
            }
        }

        // Used by Clash only currently
        // It just informs their users to check their e-mail
        private void InformTwoStep()
        {
            var sb = new StringBuilder();

            var accountsToTwoStepAuth = loginHandler.AccountsToTwoStepAuth;
            if (accountsToTwoStepAuth.Count == 0)
            {
                return;
            }

            foreach (var account in accountsToTwoStepAuth)
            {
                sb.AppendLine(account.Toon);
            }

            MessageBox.Show("Some toons require Toonstep:\n\n" + sb, "Toonstep", MessageBoxButtons.OK, MessageBoxIcon.Information);
            accountsToTwoStepAuth.Clear();
        }

        private async Task LoginAccounts()
        {
            serverMenuItem.Enabled = false;

            await loginHandler.LoginAll(currentAccountList);
            if (config.GameServer == Server.REWRITTEN)
            {
                await HandleTwoStep();
            }
            else
            {
                InformTwoStep();
            }

            foreach (var account in currentAccountList)
            {
                if (account.LoggedIn)
                {
                    gameHandler.StartGame(account);
                }
            }
            serverMenuItem.Enabled = true;
        }

        // Not aware of a good way to bind cell color to a boolean value
        // This mainly triggers by ListChanged which is triggered by
        // NotifyPropertyChanged
        public void ChangeEndCellColor(int index, Color color)
        {
            if (index < 0)
            {
                return;
            }

            // We can run this without actually selecting the cell itself
            // We need to set it to make edits on it though
            if (accountGrid.CurrentCell == null)
            {
                accountGrid.CurrentCell = accountGrid.Rows[index].Cells[End.Index];
            }

            accountGrid.BeginEdit(false);

            var chkCell = accountGrid.Rows[index].Cells[End.Index] as DataGridViewCheckBoxCell;
            chkCell.Style.BackColor = color;
            chkCell.Style.SelectionBackColor = color;

            if (!config.SelectEndGames)
            {
                chkCell.Value = chkCell.FalseValue;
            }

            accountGrid.EndEdit();
        }

        // Runs after the account list is changed.
        private void AccGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Trigger when the user changes servers
            // Logged in indicators have to be changed again
            if (e.ListChangedType == ListChangedType.Reset)
            {
                CheckLoggedIns(currentAccountList);
            }

            gridHandler.DataBindingComplete(accountGrid);
        }

        // Allows sorting the Toon column
        private void AccGrid_OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var data = currentAccountList;
            if (e.RowIndex < 0 && e.ColumnIndex == Toon.Index)
            {
                data.ApplySort();
            }
        }

        // Runs after NotifyPropertyChanged, see Account class
        private void BindingSource_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.NewIndex >= 0 && e.ListChangedType == ListChangedType.ItemChanged)
            {
                var account = currentAccountList[e.NewIndex];
                var color = account.LoggedIn ? Color.Green : Color.Red;
                ChangeEndCellColor(e.NewIndex, color);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DataProtection.MakeEntropy();

            dataHandler.SaveConfig(config, "Config.json");
            dataHandler.SaveAccounts(rewrittenAccountList, "AccListRewritten.nully");
            dataHandler.SaveAccounts(clashAccountList, "AccListClash.nully");

            base.OnFormClosing(e);
        }

        private void AccGrid_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            gridHandler.UserDeletingRow(e.Row.DataBoundItem);
        }

        private void AccGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != ToonSlots.Index)
            {
                return;
            }

            gridHandler.CellValueChanged(accountGrid, e.RowIndex);
        }

        // Runs after account info is returned from the editing form
        private void AccountEditComplete(dynamic account, int index)
        {
            currentAccountList[index] = account;

            if (account is ClashAccount && account.Authorized)
            {
                dataHandler.FindClashIngameToons(account); 
            }

            bindingSource.ResetBindings(false);
        }

        // Handles account editing and game ending
        private void AccGrid_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != End.Index && e.ColumnIndex != Toon.Index)
            {
                return;
            }

            var account = currentAccountList[e.RowIndex];

            if (e.ColumnIndex == Toon.Index && !accountGrid.moveMode)
            {
                var accountEdit = new AccountEdit(account, e.RowIndex);
                accountEdit.Edited += AccountEditComplete;
                accountEdit.ShowDialog();
            } 
            else
            {
                if (!config.SelectEndGames && account.LoggedIn)
                {
                    gameHandler.StopGame(account);
                }
            }
        }

        private void AccGrid_DragDrop(object sender, DragEventArgs e)
        {
            if (accountGrid.rowIndexToDrop < 0)
            {
                return;
            }

            var accountToMove = currentAccountList[accountGrid.rowIndexToDrop];
            var color = accountToMove.LoggedIn ? Color.Green : Color.Red;
            ChangeEndCellColor(accountGrid.rowIndexToDrop, color);
        }

        private void EndSelected_Click(object sender, EventArgs e)
        {
            foreach (var account in currentAccountList)
            {
                if (account != null && account.EndWanted && account.LoggedIn)
                {
                    gameHandler.StopGame(account);
                    account.EndWanted = false;
                }
            }
        }

        private void EndAll_Click(object sender, EventArgs e)
        {
            if (config.GlobalEndAll || config.GameServer == Server.REWRITTEN)
            {
                foreach (var acc in rewrittenAccountList)
                {
                    rewrittenGameHandler.StopGame(acc);
                }
            }
            
            if (config.GlobalEndAll || config.GameServer == Server.CLASH)
            {
                foreach (var acc in clashAccountList)
                {
                    clashGameHandler.StopGame(acc);
                }
            }
        }

        private void UntickAll_Click(object sender, EventArgs e)
        {
            foreach (var account in currentAccountList)
            {
                account.LoginWanted = false;
            }
            accountGrid.ClearGridSelections();
        }

        public void SelectionOptionAltered()
        {
            if (config.SelectEndGames)
            {
                endSelectedMenuItem.Visible = true;
                accountGrid.Columns[End.Index].ReadOnly = false;
            }
            else
            {
                endSelectedMenuItem.Visible = false;
                accountGrid.Columns[End.Index].ReadOnly = true;
                foreach (DataGridViewRow row in accountGrid.Rows)
                {
                    var chk = (DataGridViewCheckBoxCell)row.Cells[End.Index];
                    chk.Value = false;
                }
            }
        }

        private void TopMenu_Click(object sender, EventArgs e)
        {
            accountGrid.ClearGridSelections();
        }

        private async void Rewritten_Click(object sender, EventArgs e)
        {
            config.GameServer = Server.REWRITTEN;
            HandleConfig();

            bindingSource.DataSource = rewrittenAccountList;
            bindingSource.ResetBindings(false);

            await StartUpdate();
        }

        private async void Clash_Click(object sender, EventArgs e)
        {
            config.GameServer = Server.CLASH;
            HandleConfig();

            bindingSource.DataSource = clashAccountList;
            bindingSource.ResetBindings(false);

            await StartUpdate();
        }

        private void CheckLoggedIns(dynamic accountList)
        {
            for (int i = 0; i < accountList.Count; ++i)
            {
                if (accountList[i].LoggedIn)
                {
                    ChangeEndCellColor(i, Color.Green);
                }
            }
        }

        private void Options_Click(object sender, EventArgs e)
        {
            Options optionWnd = new Options(this, config);
            optionWnd.ShowDialog();
        }

        private void MoveRows_Click(object sender, EventArgs e)
        {
            currentAccountList.RemoveSort();

            if (!moveRowsMenuItem.Checked)
            {
                accountGrid.Columns[Toon.Index].ReadOnly = true;
                moveRowsMenuItem.Text = "Apply";
            }
            else
            {
                accountGrid.Columns[Toon.Index].ReadOnly = false;
                moveRowsMenuItem.Text = "Move Rows";
            }

            if (!config.SelectEndGames)
            {
                endSelectedMenuItem.Visible = false;
            }

            accountGrid.moveMode = moveRowsMenuItem.Checked = !moveRowsMenuItem.Checked;

            accountGrid.ClearGridSelections();
        }
    }
}
