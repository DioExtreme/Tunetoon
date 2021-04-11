using System;
using System.Linq;
using System.Windows.Forms;
using Tunetoon.Accounts;

namespace Tunetoon.Grid
{
    public class ClashGridHandler : IGridHandler
    {
        private ClashAuthorization clashAuthorization = new ClashAuthorization();
        public void CellValueChanged(AccountGrid grid, int rowIndex)
        {
            DataGridViewRow row = grid.Rows[rowIndex];
            if (!row.IsNewRow)
            {
                var account = row.DataBoundItem as ClashAccount;
                var cb = row.Cells["ToonSlots"] as DataGridViewComboBoxCell;
                int index = Convert.ToInt32(cb.Value);
                if (index != -1)
                {
                    account.ToonSlot = index;
                }
            }
        }

        // Assigns all ingame toons the game client has saved
        public void DataBindingComplete(AccountGrid grid)
        {
            int mainMenu = -2;

            foreach (DataGridViewRow row in grid.Rows)
            {
                var account = row.DataBoundItem as ClashAccount;

                if (account == null)
                {
                    continue;
                }

                var cell = row.Cells["ToonSlots"] as DataGridViewComboBoxCell;

                cell.DataSource = account.ClashIngameToons.ToArray();
                cell.ValueMember = "Key";
                cell.DisplayMember = "Value";

                if (account.ToonSlot != mainMenu)
                {
                    if (account.ClashIngameToons.ContainsKey(account.ToonSlot))
                        cell.Value = account.ToonSlot;
                    else
                        cell.Value = mainMenu;
                }
                else
                {
                    cell.Value = mainMenu;
                }
            }
        }

        public void UserDeletingRow(object dataBoundItem)
        {
            var account = dataBoundItem as ClashAccount;

            if (account != null && account.Authorized)
            {
                clashAuthorization.RemoveAccount(account);
            }
        }
    }
}
