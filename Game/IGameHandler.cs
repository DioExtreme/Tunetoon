using System.ComponentModel;
using System.Diagnostics;
using Tunetoon.Accounts;
using Tunetoon.Login;

namespace Tunetoon.Game
{
    public interface IGameHandler<T> where T : Account
    {
        void OnProcessExit(T acc, Process gameProcess);
        void SetupBaseEnvVariables(ILoginResult result, Process gameProcess);

        void StartGame(T account);
        void StartGame(T account, Process gameProcess, string path, string processName);

        void StopGame(T account);
        void StopGame(T account, Process gameProcess);

        void StartGameForLoggedInAccounts(BindingList<T> accountList);
    }
}
