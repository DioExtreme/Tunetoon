using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    public class ClashLoginHandler : LoginHandlerBase<ClashAccount>
    {
        private ClashAuthorization clashAuthorization = new ClashAuthorization();

        public override void GetAuthResponse(ClashAccount account)
        {
            if (!account.Authorized)
            {
                clashAuthorization.AddAccount(account);
            }

            if (!account.Authorized)
            {
                return;
            }

            clashAuthorization.LoginAccount(account);

            HandleAuthResponse(account);
        }

        public override void HandleAuthResponse(ClashAccount account)
        {
            if (account.LoginResult.Status)
            {
                account.LoggedIn = true;
            }
            else if (account.LoginResult.Toonstep)
            {
                AccountsToTwoStepAuth.Add(account);
            }
            else if (account.LoginResult.BadToken)
            {
                account.Authorized = false;
                account.LoginToken = null;
            }
        }

        #pragma warning disable 1998
        public override async Task HandleTwoStep()
        {
            var sb = new StringBuilder();

            if (AccountsToTwoStepAuth.Count == 0)
            {
                return;
            }

            foreach (var account in AccountsToTwoStepAuth)
            {
                sb.AppendLine(account.Toon);
            }

            MessageBox.Show("Some toons require Toonstep:\n\n" + sb, "Toonstep", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AccountsToTwoStepAuth.Clear();
        }
    }
}
