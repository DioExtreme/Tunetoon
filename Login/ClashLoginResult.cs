using Newtonsoft.Json;

namespace Tunetoon.Login
{
    public class ClashLoginResult : ILoginResult
    {
        public bool Status;
        public bool Toonstep;
        public int Reason;
        public string Token;

        [JsonProperty(PropertyName = "bad_token")]
        public bool BadToken;

        public string GameServer => "gs.corporateclash.net";

        public string Cookie => Token;
    }
}
