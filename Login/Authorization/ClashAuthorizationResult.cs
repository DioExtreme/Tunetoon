using Newtonsoft.Json;

namespace Tunetoon.Login.Authorization
{
    public class ClashAuthorizationResult
    {
        public bool Status;
        public int Reason;
        public string Friendlyreason;
        public string Message;
        public string Token;

        [JsonProperty(PropertyName = "bad_token")]
        public bool BadToken;
    }
}
