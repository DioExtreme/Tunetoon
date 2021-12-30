using System;
using System.Windows.Forms;
using Tunetoon.Accounts;

namespace Tunetoon.Forms
{
    public delegate void EditComplete(dynamic account, int index);
    public partial class AccountEdit : Form
    {
        public event EditComplete Edited;

        private dynamic account;
        private int index;

        public AccountEdit()
        {
            InitializeComponent();
        }

        public void StartEdit(dynamic account, int index)
        {
            FriendlyBox.Text = account.Toon;
            UsernameBox.Text = account.Username;
            PasswordBox.Text = account.Password;

            this.account = account;
            this.index = index;

            ShowDialog();
        }
        private void HandleAccount(RewrittenAccount account)
        {
            account.Username = UsernameBox.Text;
            account.Password = PasswordBox.Text;
            Edited(account, index);
            Close();
        }

        private void HandleAccount(ClashAccount account)
        {
            if (account.Authorized && account.Username == UsernameBox.Text && account.Password == PasswordBox.Text)
            {
                Edited(account, index);
                Close();
                return;
            }

            account.Username = UsernameBox.Text;
            account.Password = PasswordBox.Text;

            DoneButton.Text = "Authorizing...";
            DoneButton.Enabled = false;

            var clashAuthorization = new ClashAuthorization();
            clashAuthorization.AddAccount(account);

            if (clashAuthorization.LastReason != 0)
            {
                DoneButton.Text = "Done";
                DoneButton.Enabled = true;
                MessageBox.Show(clashAuthorization.LastMessage, "Server response", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Edited(account, index);
            Close();
        }

        private void DoneButton_Click(object sender, EventArgs e)
        {
            account.Toon = FriendlyBox.Text;
            HandleAccount(account);
        }
    }
}
