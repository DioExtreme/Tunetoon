using System.ComponentModel;
using System.Threading.Tasks;
using Tunetoon.Accounts;

namespace Tunetoon.Login
{
    interface ILoginHandler<T> where T : Account
    {
        Task RequestLogin(BindingList<T> accountList);
        Task HandleTwoStep();
        Task LoginAccounts(BindingList<T> accountList);
        void GetAuthResponse(T account);
        void HandleAuthResponse(T account);
    }
}
