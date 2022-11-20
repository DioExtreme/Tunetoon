using System.Text.Json.Serialization;

namespace Tunetoon.Patcher
{
    public class RewrittenPatch
    {
        // Retrieved from manifest
        [JsonPropertyName("fileName")]
        public string Filename;
        [JsonPropertyName("compPatchHash")]
        public string CompPatchHash;
        [JsonPropertyName("patchHash")]
        public string PatchHash;

        // Added during file scanning
        public string FinalFileHash;
    }
}
