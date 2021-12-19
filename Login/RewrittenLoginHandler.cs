using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Tunetoon.Accounts;
using Tunetoon.Forms;

namespace Tunetoon.Login
{
    public class RewrittenLoginHandler : LoginHandlerBase<RewrittenAccount>
    {
        private HttpClient httpClient = Program.HttpClient;
        private const string RewrittenBaseAddress = "https://www.toontownrewritten.com";

        // See: https://github.com/ToontownRewritten/api-doc/blob/master/login.md
        public override void GetAuthResponse(RewrittenAccount account)
        {
            var result = account.LoginResult;
            var status = account.LoginResult.Success;

            var data = new Dictionary<string, string>();

            using (var request = new HttpRequestMessage(HttpMethod.Post, RewrittenBaseAddress + "/api/login?format=json"))
            {
                if (status == null || status == "true" || status == "false")
                {
                    data.Add("username", account.Username);
                    data.Add("password", account.Password);
                }
                else if (status == "partial")
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

                request.Content = new FormUrlEncodedContent(data);
                var response = httpClient.SendAsync(request).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                account.LoginResult = JsonConvert.DeserializeObject<RewrittenLoginResult>(responseString);
            }

            HandleAuthResponse(account);
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
            GetAuthResponse(account);
        }

        private async Task RequestLoginAfterTwoStep()
        {
            var tasks = new List<Task>();
            var authList = new List<RewrittenAccount>(AccountsToTwoStepAuth);

            foreach (var account in authList)
            {
                string authCode = account.LoginResult.AuthToken;
                if (authCode == null || authCode.Equals("ToonGuard") || authCode.Equals("Two-Step Token"))
                {
                    continue;
                }
                AccountsToTwoStepAuth.Remove(account);
                tasks.Add(Task.Run(() => GetAuthResponse(account)));
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
