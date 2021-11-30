using System.Diagnostics;
using Tunetoon.Accounts;
using Tunetoon.Login;

namespace Tunetoon.Game
{
    public class RewrittenGameHandler : GameHandlerBase<RewrittenAccount>
    {
        private Config config;

        public RewrittenGameHandler(Config config)
        {
            this.config = config;
        }

        public override void SetupBaseEnvVariables(ILoginResult result, Process gameProcess)
        {
            gameProcess.StartInfo.EnvironmentVariables["TTR_GAMESERVER"] = result.GameServer;
            gameProcess.StartInfo.EnvironmentVariables["TTR_PLAYCOOKIE"] = result.Cookie;
        }

        public override void StartGame(RewrittenAccount account)
        {
            if (account.LoggedIn)
            {
                var gameProcess = new Process();
                SetupBaseEnvVariables(account.LoginResult, gameProcess);
                StartGame(account, gameProcess, config.RewrittenPath, "TTREngine64.exe");
            }
        }
    }
}
