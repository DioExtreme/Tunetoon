using System.Text.Json.Serialization;

namespace Tunetoon.Patcher
{
    public class RewrittenPatch
    {
        // Retrieved from manifest
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
        [JsonPropertyName("compPatchHash")]
        public string CompPatchHash { get; set; }
        [JsonPropertyName("patchHash")]
        public string PatchHash { get; set; }

        // Added during file scanning
        public string FinalFileHash;
    }
}
