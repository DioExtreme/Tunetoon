using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tunetoon.Patcher
{
    public class RewrittenFile
    {
        [JsonPropertyName("dl")]
        public string Dl { get; set; }
        [JsonPropertyName("only")]
        public List<string> Only { get; set; }
        [JsonPropertyName("hash")]
        public string Hash { get; set; }
        [JsonPropertyName("compHash")]
        public string CompHash { get; set; }
        [JsonPropertyName("patches")]
        public Dictionary<string, RewrittenPatch> Patches { get; set; }
    }
}
