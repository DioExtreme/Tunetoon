using Newtonsoft.Json;
using static System.Environment;

namespace Tunetoon
{
     public enum Server { REWRITTEN, CLASH }

     public class Config
     {  
        [JsonIgnore]
        public const int LauncherVersion = 60;

        public Server GameServer = Server.REWRITTEN;

        public string RewrittenPath { get; set; } = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Toontown Rewritten\\";

        public string ClashPath { get; set; } = GetFolderPath(SpecialFolder.LocalApplicationData) + "\\Corporate Clash\\";

        public string ClashDistrict;

        public bool SkipUpdates { get; set; }

        public bool SelectEndGames { get; set; }

        public bool GlobalEndAll { get; set; }
     }
}
