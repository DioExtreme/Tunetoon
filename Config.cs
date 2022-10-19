using static System.Environment;

namespace Tunetoon
{
     public enum Server { Rewritten, Clash }

     public class Config
     {  
        public Server GameServer = Server.Rewritten;

        public bool EncryptAccounts { get; set;} = true;

        public string RewrittenPath { get; set; } = GetFolderPath(SpecialFolder.ProgramFilesX86) + "\\Toontown Rewritten\\";

        public string ClashPath { get; set; } = GetFolderPath(SpecialFolder.LocalApplicationData) + "\\Corporate Clash\\";

        public bool SkipUpdates { get; set; }

        public bool SelectEndGames { get; set; }

        public bool GlobalEndAll { get; set; }
     }
}
