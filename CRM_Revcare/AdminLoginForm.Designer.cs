using System.Windows.Forms;
namespace CRM_Revcare_
{
    partial class AdminLoginForm
    {
#pragma warning disable CS0649
        private System.ComponentModel.IContainer components;
#pragma warning restore CS0649
        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            systemName = new Label();
            loginTitle = new Label();
            usernameLabel = new Label();
            username = new TextBox();
            passwordLabel = new Label();
            password = new TextBox();
            login = new Button();
            status = new Label();
            SuspendLayout();
            // 
            // systemName
            // 
            systemName.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            systemName.Location = new System.Drawing.Point(26, 31);
            systemName.Name = "systemName";
            systemName.Size = new System.Drawing.Size(343, 47);
            systemName.TabIndex = 0;
            systemName.Text = "RevCare Auto Hub CRM";
            // 
            // loginTitle
            // 
            loginTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
            loginTitle.Location = new System.Drawing.Point(26, 88);
            loginTitle.Name = "loginTitle";
            loginTitle.Size = new System.Drawing.Size(331, 33);
            loginTitle.TabIndex = 1;
            loginTitle.Text = "Login";
            // 
            // usernameLabel
            // 
            usernameLabel.Location = new System.Drawing.Point(26, 144);
            usernameLabel.Name = "usernameLabel";
            usernameLabel.Size = new System.Drawing.Size(331, 27);
            usernameLabel.TabIndex = 2;
            usernameLabel.Text = "Username";
            // 
            // username
            // 
            username.Location = new System.Drawing.Point(26, 175);
            username.Margin = new Padding(3, 4, 3, 4);
            username.Name = "username";
            username.Size = new System.Drawing.Size(331, 27);
            username.TabIndex = 3;
            // 
            // passwordLabel
            // 
            passwordLabel.Location = new System.Drawing.Point(26, 227);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new System.Drawing.Size(331, 27);
            passwordLabel.TabIndex = 4;
            passwordLabel.Text = "Password";
            // 
            // password
            // 
            password.Location = new System.Drawing.Point(26, 257);
            password.Margin = new Padding(3, 4, 3, 4);
            password.Name = "password";
            password.Size = new System.Drawing.Size(331, 27);
            password.TabIndex = 5;
            password.UseSystemPasswordChar = true;
            // 
            // login
            // 
            login.Location = new System.Drawing.Point(26, 313);
            login.Margin = new Padding(3, 4, 3, 4);
            login.Name = "login";
            login.Size = new System.Drawing.Size(331, 45);
            login.TabIndex = 6;
            login.Text = "Log in";
            // 
            // status
            // 
            status.Location = new System.Drawing.Point(26, 373);
            status.Name = "status";
            status.Size = new System.Drawing.Size(331, 87);
            status.TabIndex = 7;
            // 
            // AdminLoginForm
            // 
            AcceptButton = login;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(389, 507);
            Controls.Add(systemName);
            Controls.Add(loginTitle);
            Controls.Add(usernameLabel);
            Controls.Add(username);
            Controls.Add(passwordLabel);
            Controls.Add(password);
            Controls.Add(login);
            Controls.Add(status);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AdminLoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "RevCare Auto Hub CRM - Login";
            Load += AdminLoginForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Label systemName;
        private Label loginTitle;
        private Label usernameLabel;
        private TextBox username;
        private Label passwordLabel;
        private TextBox password;
        private Button login;
        private Label status;
    }
}
