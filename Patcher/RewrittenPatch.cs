namespace Tunetoon.Patcher
{
    public class RewrittenPatch
    {
        // Retrieved from manifest
        public string Filename;
        public string CompPatchHash;
        public string PatchHash;

        // Added during file scanning
        public string FinalFileHash;
    }
}
