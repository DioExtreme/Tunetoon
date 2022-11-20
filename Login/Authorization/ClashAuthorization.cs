using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Tunetoon.Accounts;
using Tunetoon.Login;
using Tunetoon.Login.Authorization;

namespace Tunetoon
{
    public class ClashAuthorization
    {
        private Random random = new Random();

        private HttpClient httpClient = Program.HttpClient;
        private const string ClashBaseAddress = "https://corporateclash.net";

        public int LastReason;
        public string LastMessage;

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var charArray = new char[length];

            for (int i = 0; i < charArray.Length; i++)
            {
                charArray[i] = chars[random.Next(chars.Length)];
            }

            return new string(charArray);
        }

        // Everything below here is undocumented and taken
        // from the official launcher through memory dumps.
        // Therefore, routes may or may not break in the future.

        public void AddAccount(ClashAccount account)
        {
            string friendly = "Tunetoon-" + RandomString(16);
            var data = new Dictionary<string, string>();

            using (var request = new HttpRequestMessage(HttpMethod.Post, ClashBaseAddress +  "/api/launcher/v1/register"))
            {
                data.Add("username", account.Username);
                data.Add("password", account.Password);
                // Friendly is a way of saying "Launcher nickname"
                // It also has to be unique compared to previous entries
                data.Add("friendly", friendly);

                request.Content = new FormUrlEncodedContent(data);

                var response = httpClient.SendAsync(request).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;

                var authObject = JsonSerializer.Deserialize<ClashAuthorizationResult>(responseString);

                if (authObject.Status)
                {
                    account.Authorized = true;
                    account.LoginToken = authObject.Token;
                }
                else
                {
                    account.Authorized = false;
                    account.LoginToken = null;
                    LastMessage = authObject.Message;
                }
                LastReason = authObject.Reason;
            }
        }

        public void LoginAccount(ClashAccount account)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, ClashBaseAddress + "/api/launcher/v1/login"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.LoginToken);
                request.Headers.Add("x-realm", "production");

                var response = httpClient.SendAsync(request).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                account.LoginResult = JsonSerializer.Deserialize<ClashLoginResult>(responseString);
            }
        }

        // This requires an authenticated account to use
        // We just use the mirror URL directly
        // Provided for reference
        public void GetMetadata(ClashAccount account)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, ClashBaseAddress + "/api/launcher/v1/metadata"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.LoginToken);

                var response = httpClient.SendAsync(request).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
            }
        }

        public void RemoveAccount(ClashAccount account)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, ClashBaseAddress +  "/api/launcher/v1/revoke_self"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.LoginToken);

                var response = httpClient.SendAsync(request).Result;
                // Don't care
                string responseString = response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
