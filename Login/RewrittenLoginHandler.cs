using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tunetoon.Accounts;
using Tunetoon.Forms;

namespace Tunetoon.Login
{
    public class RewrittenLoginHandler : LoginHandlerBase<RewrittenAccount>
    {
        private HttpClient httpClient = Program.HttpClient;
        private const string RewrittenBaseAddress = "https://www.toontownrewritten.com";

        // See: https://github.com/ToontownRewritten/api-doc/blob/master/login.md

        private void GetAndHandleLoginResult(RewrittenAccount account, Dictionary<string, string> data)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, RewrittenBaseAddress + "/api/login?format=json"))
            {
                request.Content = new FormUrlEncodedContent(data);
                var response = httpClient.SendAsync(request).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                account.LoginResult = JsonSerializer.Deserialize<RewrittenLoginResult>(responseString);
            }
            HandleAuthResponse(account);
        }

        public override void GetAuthResponse(RewrittenAccount account)
        {
            var data = new Dictionary<string, string>();

            data.Add("username", account.Username);
            data.Add("password", account.Password);

            GetAndHandleLoginResult(account, data);
        }

        private void GetAuthResponseAfterPartial(RewrittenAccount account)
        {
            var result = account.LoginResult;
            var status = result.Success;

            var data = new Dictionary<string, string>();

            if (status == "partial")
            {
                data.Add("appToken", result.AuthToken);
                data.Add("authToken", result.ResponseToken);
            }
            else if (status == "delayed")
            {
                data.Add("queueToken", result.QueueToken);
            }
            else
            {
                return;
            }

            GetAndHandleLoginResult(account, data);
        }

        public override void HandleAuthResponse(RewrittenAccount account)
        {
            var result = account.LoginResult;
            var status = result.Success;

            if (status == "true")
            {
                account.LoggedIn = true;
                return;
            }

            if (status == "false")
            {
                return;
            }

            if (status == "partial")
            { 
                result.AuthToken = result.Banner.Contains("ToonGuard") ? "ToonGuard" : "Two-Step Token";
                AccountsToTwoStepAuth.Add(account);
                return;
            }

            GetAuthResponseAfterPartial(account);
        }

        private bool NoTwoStepTokenGiven(string authToken)
        {
            return authToken == null || authToken.Equals("ToonGuard") || authToken.Equals("Two-Step Token");
        }

        private async Task RequestLoginAfterTwoStep()
        {
            var tasks = new List<Task>();
            foreach (var account in AccountsToTwoStepAuth.ToArray())
            {
                string authToken = account.LoginResult.AuthToken;
                if (NoTwoStepTokenGiven(authToken))
                {
                    continue;
                }
                AccountsToTwoStepAuth.Remove(account);
                tasks.Add(Task.Run(() => GetAuthResponseAfterPartial(account)));
            }
            await Task.WhenAll(tasks);
        }

        public override async Task HandleTwoStep()
        {
            while (AccountsToTwoStepAuth.Count > 0)
            {
                bool authCancelled = false;
                var authForm = new Auth(AccountsToTwoStepAuth);
                authForm.AuthTokensEntered += (accountsToAuth) => AccountsToTwoStepAuth = accountsToAuth;
                authForm.IsClosed += () => authCancelled = true;
                authForm.StartPosition = FormStartPosition.CenterParent;
                authForm.ShowDialog();

                if (authCancelled)
                {
                    break;
                }

                await RequestLoginAfterTwoStep();
            }
        }
    }
}
