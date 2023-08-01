using System.Text.Json.Serialization;

namespace Tunetoon.DioExtreme
{
    public class ClashUrls
    {
        [JsonIgnore]
        public bool Initialized { get; set; } = false;
        [JsonPropertyName("gameserver")]
        public string GameServer { get; set; }
        [JsonPropertyName("patchserver")]
        public string PatchServer { get; set; }
        [JsonPropertyName("main_manifest")]
        public string MainManifest { get; set; }
        [JsonPropertyName("resource_manifest")]
        public string ResourceManifest { get; set; }
    }
}
