using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    public class ClashLoginHandler : LoginHandlerBase<ClashAccount>
    {
        private HttpClient httpClient = Program.httpClient;
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
            else if (account.LoginResult.bad_token)
            {
                account.Authorized = false;
                account.LoginToken = null;
            }
        }
    }
}
