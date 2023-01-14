using System.Threading.Tasks;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    interface ILoginHandler<T> where T : Account
    {
        Task RequestLogin(AccountList<T> accountList);
        Task HandleTwoStep();
        Task LoginAccounts(AccountList<T> accountList);
        void GetAuthResponse(T account);
        void HandleAuthResponse(T account);
    }
}
