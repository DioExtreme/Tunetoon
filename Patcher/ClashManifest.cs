using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tunetoon.Patcher
{
    public class ClashManifest
    {
        [JsonPropertyName("files")]
        public List<ClashFile> Files { get; set; }
    }
}
