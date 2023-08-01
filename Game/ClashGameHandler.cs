using System.Diagnostics;
using Tunetoon.Accounts;
using Tunetoon.Login;

namespace Tunetoon.Game
{
    public class ClashGameHandler : GameHandlerBase<ClashAccount>
    {
        private Config config;
        

        public ClashGameHandler(Config config)
        {
            this.config = config;
        }

        public override void SetupBaseEnvVariables(ILoginResult result, Process gameProcess)
        {
            gameProcess.StartInfo.EnvironmentVariables["TT_GAMESERVER"] = config.ClashUrls.GameServer;
            gameProcess.StartInfo.EnvironmentVariables["TT_PLAYCOOKIE"] = result.Cookie;
        }

        private void SetupClashEnvVariables(ClashAccount account, Process gameProcess)
        {
            gameProcess.StartInfo.EnvironmentVariables["LAUNCHER_USER"] = account.Username;

            gameProcess.StartInfo.EnvironmentVariables["REALM"] = "production";
            gameProcess.StartInfo.EnvironmentVariables["SENTRY_ENVIRONMENT"] = "production";

            if (account.ToonSlot != -2)
            {
                gameProcess.StartInfo.EnvironmentVariables["FORCE_TOON_SLOT"] = account.ToonSlot.ToString();
            }
        }

        public override void StartGame(ClashAccount account)
        {
            if (account.LoggedIn)
            {
                var gameProcess = new Process();
                SetupBaseEnvVariables(account.LoginResult, gameProcess);
                SetupClashEnvVariables(account, gameProcess);
                StartGame(account, gameProcess, config.ClashPath, "CorporateClash.exe");
            }
        }
    }
}
