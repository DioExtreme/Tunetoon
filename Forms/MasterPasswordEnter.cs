using System.Windows.Forms;
using Tunetoon.Encrypt;

namespace Tunetoon.Forms
{
    
    public partial class MasterPasswordEnter : Form
    {
        private AccountDecryptor accountDecryptor;
        public MasterPasswordEnter(AccountDecryptor accountDecryptor)
        {
            this.accountDecryptor = accountDecryptor;
            InitializeComponent();
        }

        private void MasterPasswordEnter_Load(object sender, System.EventArgs e)
        {
            if (accountDecryptor.isComingFromV1())
            {
                MessageBox.Show("Tunetoon v2 does not support accounts with encryption on from v1.\n\n" +
                    "Launch v1.7, disable encryption and close the program.\n\n" +
                    "Then, launch v2 again and re-enable the new encryption mode. Note that v2's encryption is incompatitble with v1.", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                Application.Exit();
                return;
            }
        }

        private void startButton_Click(object sender, System.EventArgs e)
        {
            string masterPassword = passwordTextbox.Text;

            if (string.IsNullOrEmpty(masterPassword)) 
            {
                return;
            }

            try
            {
                accountDecryptor.AuthenticateEncrypted(masterPassword);
                Close();
            }
            catch
            {
                MessageBox.Show("The password is invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }       
        }
    }
}
