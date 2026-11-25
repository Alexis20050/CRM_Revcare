using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    public sealed partial class AdminLoginForm : Form
    {
        private readonly AdminAuthentication authentication;
        private bool busy;
        private readonly Func<string, string, Task<bool>> apiLogin;

        internal AdminLoginForm(Func<string, string, Task<bool>> loginAction) : this(new AdminAuthentication())
        {
            apiLogin = loginAction;
        }

        public AdminLoginForm() : this(new AdminAuthentication()) { }
        public AdminLoginForm(AdminAuthentication authentication)
        {
            this.authentication = authentication;
            InitializeComponent();
            login.Click += async (s, e) => await LogInAsync();
            FormClosing += (s, e) => { if (busy) e.Cancel = true; };
        }

        private async Task LogInAsync()
        {
            if (busy) return;
            if (string.IsNullOrWhiteSpace(username.Text) || password.Text.Length == 0)
            {
                status.ForeColor = Color.Red;
                status.Text = "Enter your username and password.";
                return;
            }
            busy = true; login.Enabled = false; username.Enabled = false; password.Enabled = false;
            status.ForeColor = SystemColors.ControlText;
            status.Text = "";
            string enteredName = username.Text, enteredPassword = password.Text;
            bool authenticated = false;
            try
            {
                authenticated = apiLogin != null ? await apiLogin(enteredName, enteredPassword) : await Task.Run(() =>
                {
                    authentication.EnsureAdminAccount();
                    return authentication.Validate(enteredName, enteredPassword);
                });
                if (authenticated)
                {
                    status.ForeColor = Color.Green;
                    status.Text = "Signed in.";
                }
                else
                {
                    status.ForeColor = Color.Red;
                    status.Text = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                status.ForeColor = Color.Red;
                bool sqlFailure = false;
                for (Exception cause = ex; cause != null; cause = cause.InnerException)
                    if (cause is Microsoft.Data.SqlClient.SqlException) sqlFailure = true;
                status.Text = apiLogin != null ? ex.Message : sqlFailure
                    ? "Cannot connect to the database. Check your internet and database availability, then retry Login."
                    : "Login could not finish. Check the database configuration and try again.";
            }
            finally { busy = false; login.Enabled = true; username.Enabled = true; password.Enabled = true; password.Clear(); }
            if (authenticated)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                password.Focus();
            }
        }

        private void AdminLoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}
