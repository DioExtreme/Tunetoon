using System.Text.Json.Serialization;

namespace Tunetoon.Login
{
    public sealed class RewrittenLoginResult : ILoginResult
    {
        [JsonPropertyName("success")]
        public string Success { get; set; }
        [JsonPropertyName("gameserver")]
        public string GameServer { get; set; }
        [JsonPropertyName("cookie")]
        public string Cookie { get; set; }
        [JsonPropertyName("banner")]
        public string Banner { get; set; }
        [JsonPropertyName("responseToken")]
        public string ResponseToken { get; set; }
        [JsonPropertyName("queueToken")]
        public string QueueToken { get; set; }
        public string AuthToken { get; set; }

        string ILoginResult.Cookie => Cookie;
    }
}
