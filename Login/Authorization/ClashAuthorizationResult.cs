using System.Text.Json.Serialization;

namespace Tunetoon.Login.Authorization
{
    public class ClashAuthorizationResult
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        [JsonPropertyName("reason")]
        public int Reason { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("bad_token")]
        public bool BadToken { get; set; }
    }
}
