using System.Text.Json.Serialization;

namespace Tunetoon.Login
{
    public class ClashLoginResult : ILoginResult
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        [JsonPropertyName("toonstep")]
        public bool Toonstep { get; set; }
        [JsonPropertyName("reason")]
        public int Reason { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("bad_token")]
        public bool BadToken { get; set; }

        public string GameServer => "";

        public string Cookie => Token;
    }
}
