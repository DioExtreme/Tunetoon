using System.Drawing;
using System.Windows.Forms;
using Tunetoon.Encrypt;

namespace Tunetoon.Forms
{
    public delegate void onMasterPasswordRegister(string hash);
    public delegate void onMasterPasswordRegisterCancelled();
    public partial class MasterPasswordRegister : Form
    {
        public event onMasterPasswordRegisterCancelled formCancelled;
        public MasterPasswordRegister()
        {
            InitializeComponent();
        }

        private void onFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                formCancelled();
            }
        }

        private void passwordTextbox_TextChanged(object sender, System.EventArgs e)
        {
            if (passwordConfirmTextbox.Text != passwordTextbox.Text)
            {
                passwordConfirmTextbox.ForeColor = Color.Red;
            }
            else
            {
                passwordConfirmTextbox.ForeColor = default;
            }
        }

        private void passwordConfirmTextbox_TextChanged(object sender, System.EventArgs e)
        {
            if (passwordConfirmTextbox.Text != passwordTextbox.Text)
            {
                passwordConfirmTextbox.ForeColor = Color.Red;
            }
            else
            {
                passwordConfirmTextbox.ForeColor = default;
            }
        }

        private void protectButton_Click(object sender, System.EventArgs e)
        {
            if (passwordTextbox.Text == string.Empty)
            {
                MessageBox.Show("Password cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (passwordConfirmTextbox.Text != passwordTextbox.Text)
            {
                MessageBox.Show("Passwords do not match.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult dialogResult = 
                MessageBox.Show("You will be asked to type this password every time you launch the program. " +
                "Forgetting the password makes the accounts inaccessible.\n\n" +
                "Proceed?", 
                "Info", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

            if (dialogResult == DialogResult.No)
            {
                return;
            }

            string masterPassword = passwordConfirmTextbox.Text;
            DataProtection.CreatePBKDF2(masterPassword);
            Dispose();
        }
    }
}
