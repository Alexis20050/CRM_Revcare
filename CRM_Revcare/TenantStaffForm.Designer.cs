namespace CRM_Revcare_;
partial class TenantStaffForm
{
    private System.Windows.Forms.Label usernameLabel;
    private System.Windows.Forms.TextBox usernameInput;
    private System.Windows.Forms.Label displayNameLabel;
    private System.Windows.Forms.TextBox displayNameInput;
    private System.Windows.Forms.Label roleLabel;
    private System.Windows.Forms.ComboBox roleInput;
    private System.Windows.Forms.Label isActiveLabel;
    private System.Windows.Forms.CheckBox isActiveInput;
    private System.Windows.Forms.Label initialPasswordLabel;
    private System.Windows.Forms.TextBox initialPasswordInput;
    private void InitializeComponent()
    {
        fieldsLayout.SuspendLayout();
        SuspendLayout();
        usernameLabel = new System.Windows.Forms.Label();
        usernameInput = new System.Windows.Forms.TextBox();
        usernameLabel.Name = "usernameLabel"; usernameLabel.Text = "Username *";
        usernameLabel.AutoSize = true; usernameLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        usernameInput.Name = "username"; usernameInput.Dock = System.Windows.Forms.DockStyle.Fill;
        usernameInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(usernameLabel, 0, 0); fieldsLayout.Controls.Add(usernameInput, 1, 0);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        displayNameLabel = new System.Windows.Forms.Label();
        displayNameInput = new System.Windows.Forms.TextBox();
        displayNameLabel.Name = "displayNameLabel"; displayNameLabel.Text = "Staff name *";
        displayNameLabel.AutoSize = true; displayNameLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        displayNameInput.Name = "displayName"; displayNameInput.Dock = System.Windows.Forms.DockStyle.Fill;
        displayNameInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(displayNameLabel, 0, 1); fieldsLayout.Controls.Add(displayNameInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        roleLabel = new System.Windows.Forms.Label();
        roleInput = new System.Windows.Forms.ComboBox();
        roleLabel.Name = "roleLabel"; roleLabel.Text = "Role *";
        roleLabel.AutoSize = true; roleLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        roleInput.Name = "role"; roleInput.Dock = System.Windows.Forms.DockStyle.Fill;
        roleInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        roleInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        roleInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems; roleInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        roleInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(roleLabel, 0, 2); fieldsLayout.Controls.Add(roleInput, 1, 2);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        isActiveLabel = new System.Windows.Forms.Label();
        isActiveInput = new System.Windows.Forms.CheckBox();
        isActiveLabel.Name = "isActiveLabel"; isActiveLabel.Text = "Active staff member";
        isActiveLabel.AutoSize = true; isActiveLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        isActiveInput.Name = "isActive"; isActiveInput.Dock = System.Windows.Forms.DockStyle.Fill;
        isActiveInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(isActiveLabel, 0, 3); fieldsLayout.Controls.Add(isActiveInput, 1, 3);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        initialPasswordLabel = new System.Windows.Forms.Label();
        initialPasswordInput = new System.Windows.Forms.TextBox();
        initialPasswordLabel.Name = "initialPasswordLabel"; initialPasswordLabel.Text = "Initial password";
        initialPasswordLabel.AutoSize = true; initialPasswordLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        initialPasswordInput.Name = "initialPassword"; initialPasswordInput.Dock = System.Windows.Forms.DockStyle.Fill;
        initialPasswordInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        initialPasswordInput.UseSystemPasswordChar = true;
        fieldsLayout.Controls.Add(initialPasswordLabel, 0, 4); fieldsLayout.Controls.Add(initialPasswordInput, 1, 4);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 5;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

