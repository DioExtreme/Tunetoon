using Newtonsoft.Json;
using Tunetoon.Login;

namespace Tunetoon.Accounts
{
    public class RewrittenAccount : Account
    { 
        [JsonIgnore]
        public RewrittenLoginResult LoginResult = new RewrittenLoginResult();

        [JsonIgnore]
        public string AuthToken
        {
            get { return LoginResult.AuthToken; }
            set { LoginResult.AuthToken = value; }
        }
    }
}
