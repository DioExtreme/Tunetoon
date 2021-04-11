using System.Threading.Tasks;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    interface ILoginHandler<T> where T : Account
    {
        Task LoginAll(AccountList<T> accountList);
        Task HandleTwoStep();
        void GetAuthResponse(T account);
        void HandleAuthResponse(T account);
    }
}
