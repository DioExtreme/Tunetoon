using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tunetoon.Accounts;
using Tunetoon.Login;

namespace Tunetoon.Game
{
    public class GameHandlerBase<T> : IGameHandler<T> where T : Account
    {
        public Dictionary<T, Process> ActiveProcesses = new Dictionary<T, Process>();
        public virtual void SetupBaseEnvVariables(ILoginResult result, Process gameProcess)
        {
            throw new NotImplementedException("Override for the specific gameserver.");
        }

        public virtual void OnProcessExit(T account, Process gameProcess)
        {
            ActiveProcesses.Remove(account);
            account.LoggedIn = false;   
        }

        public virtual void StartGame(T account, Process gameProcess, string path, string processName)
        {
            if (ActiveProcesses.ContainsKey(account))
            {
                return;
            }

            ActiveProcesses.Add(account, gameProcess);

            gameProcess.StartInfo.CreateNoWindow = true;
            gameProcess.StartInfo.UseShellExecute = false;
            gameProcess.EnableRaisingEvents = true;

            gameProcess.StartInfo.WorkingDirectory = path;
            // UseShellExecute = false requires the full path for FileName
            gameProcess.StartInfo.FileName = path + processName;

            gameProcess.Exited += (sender, e) => OnProcessExit(account, gameProcess);
            gameProcess.Start();

        }

        public virtual void StopGame(T account, Process gameProcess)
        {
            if (account.LoggedIn)
            {
                gameProcess.Kill();
            }
        }

        public virtual void StartGame(T account)
        {
            throw new NotImplementedException("Override for the specific gameserver.");
        }

        public virtual void StopGame(T account)
        {
            if (account.LoggedIn)
            {
                var gameProcess = ActiveProcesses[account];
                StopGame(account, gameProcess);
            }
        }
    }
}
