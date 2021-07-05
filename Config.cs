using Newtonsoft.Json;
using static System.Environment;

namespace Tunetoon
{
     public enum Server { Rewritten, Clash }

     public class Config
     {  
        [JsonIgnore]
        public const int LauncherVersion = 60;

        public Server GameServer = Server.Rewritten;

        public string RewrittenPath { get; set; } = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Toontown Rewritten\\";

        public string ClashPath { get; set; } = GetFolderPath(SpecialFolder.LocalApplicationData) + "\\Corporate Clash\\";

        public string ClashDistrict;

        public bool SkipUpdates { get; set; }

        public bool SelectEndGames { get; set; }

        public bool GlobalEndAll { get; set; }
     }
}
