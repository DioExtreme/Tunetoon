using System.IO;
using Tunetoon.Accounts;
using Tunetoon.Forms;
using Tunetoon.Utilities;

namespace Tunetoon.Encrypt
{
    public delegate void onPassedAuthentication(AccountList<RewrittenAccount> rewrittenAccountList, AccountList<ClashAccount> clashAccountList);
    public class AccountDecryptor
    {
        public event onPassedAuthentication OnPassedAuthentication;
        private DataHandler dataHandler = new DataHandler();
        public void Authenticate(Config config)
        {
            if (!config.EncryptAccounts)
            {
                var rewrittenAccountList = dataHandler.Deserialize<AccountList<RewrittenAccount>>(Constants.REWRITTEN_ACCOUNT_FILE_NAME);
                var clashAccountList = dataHandler.Deserialize<AccountList<ClashAccount>>(Constants.CLASH_ACCOUNT_FILE_NAME);

                OnPassedAuthentication(rewrittenAccountList, clashAccountList);
                return;
            }

            MasterPasswordEnter masterPassword = new MasterPasswordEnter(this);
            masterPassword.ShowDialog();
        }

        public void AuthenticateEncrypted(string masterPassword)
        {
            var rewrittenAccountList = dataHandler.LoadEncrypted<RewrittenAccount>(masterPassword, Constants.ENCRYPTED_REWRITTEN_ACCOUNT_FILE_NAME);
            var clashAccountList = dataHandler.LoadEncrypted<ClashAccount>(masterPassword, Constants.ENCRYPTED_CLASH_ACCOUNT_FILE_NAME);

            OnPassedAuthentication(rewrittenAccountList, clashAccountList);
        }
        public bool isComingFromV1()
        {
            return !File.Exists(Constants.ENCRYPTED_REWRITTEN_ACCOUNT_FILE_NAME) &&
                !File.Exists(Constants.ENCRYPTED_CLASH_ACCOUNT_FILE_NAME);
        }
    }
}
