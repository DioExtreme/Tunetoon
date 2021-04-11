using System.Collections.Generic;

namespace Tunetoon.Patcher
{
    public class RewrittenFile
    {
        public string Dl;
        public List<string> Only;
        public string Hash;
        public string CompHash;
        public Dictionary<string, RewrittenPatch> Patches;
    }
}
