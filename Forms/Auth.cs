using System;
using System.Windows.Forms;

namespace Tunetoon.Forms
{
    public delegate void CancelledHandler();

    public delegate void AuthListHandler(dynamic accountsToAuth);

    public partial class Auth : Form
    {
        public event AuthListHandler AuthTokensEntered;

        public event CancelledHandler IsClosed;

        private dynamic AccountsToAuth;

        public Auth(dynamic accountsToAuth)
        {
            AccountsToAuth = accountsToAuth;

            InitializeComponent();

            AuthGrid.AutoGenerateColumns = false;
            AuthGrid.DataSource = AccountsToAuth;
        }

        private void Auth2Button_Click(object sender, EventArgs e)
        {
            AuthGrid.CurrentCell = null;

            AuthTokensEntered(AccountsToAuth);
            Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                IsClosed();
            }
        }

        private void AuthGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == Cancel.Index)
                AuthGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void AuthGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == Cancel.Index)
            {
                if (Convert.ToBoolean(AuthGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value))
                {
                    AccountsToAuth.RemoveAt(e.RowIndex);
                    AuthGrid.DataSource = null;
                    AuthGrid.DataSource = AccountsToAuth;
                }
                if (AccountsToAuth.Count == 0)
                {
                    Dispose();
                }
            }
        }

        private void AuthGrid_MouseClick(object sender, MouseEventArgs e)
        {
            var ht = AuthGrid.HitTest(e.X, e.Y);

            if (ht.Type == DataGridViewHitTestType.None)
            {
                AuthGrid.CurrentCell = null;
            }
        }

        private void Auth_Load(object sender, EventArgs e)
        {
        }
    }
}