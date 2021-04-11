using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    public class LoginHandlerBase<T> : ILoginHandler<T> where T : Account
    {
        public List<T> AccountsToTwoStepAuth = new List<T>();
        public virtual async Task LoginAll(AccountList<T> accountList)
        {
            AccountsToTwoStepAuth.Clear();

            var tasks = new List<Task>();

            foreach (var account in accountList)
            {
                if (!account.CanLogin())
                {
                    continue;
                }
                tasks.Add(Task.Run(() => GetAuthResponse(account)));
            }
            await Task.WhenAll(tasks);
        }

        public virtual async Task HandleTwoStep()
        {
            await Task.CompletedTask;
        }

        public virtual void GetAuthResponse(T account)
        {
            throw new NotImplementedException();
        }

        public virtual void HandleAuthResponse(T account)
        {
            throw new NotImplementedException();
        }
    }
}