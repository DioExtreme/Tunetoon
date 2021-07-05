using Newtonsoft.Json;

namespace Tunetoon.Patcher
{
    public class ClashFile
    {
        public string FileName;
        public string FilePath;
        public string Sha1;

        [JsonProperty(PropertyName = "compressed_sha1")]
        public string CompressedSha1;
    }
}
