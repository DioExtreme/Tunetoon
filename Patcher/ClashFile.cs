using System.Text.Json.Serialization;

namespace Tunetoon.Patcher
{
    public class ClashFile
    {

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; }
        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; }

        [JsonPropertyName("compressed_sha1")]
        public string CompressedSha1 { get; set; }
    }
}
