namespace CRM_Revcare_;
partial class TenantCustomerForm
{
    private System.Windows.Forms.Label fullNameLabel;
    private System.Windows.Forms.TextBox fullNameInput;
    private System.Windows.Forms.Label contactNumberLabel;
    private System.Windows.Forms.TextBox contactNumberInput;
    private System.Windows.Forms.Label emailLabel;
    private System.Windows.Forms.TextBox emailInput;
    private System.Windows.Forms.Label isActiveLabel;
    private System.Windows.Forms.CheckBox isActiveInput;
    private void InitializeComponent()
    {
        fieldsLayout.SuspendLayout();
        SuspendLayout();
        fullNameLabel = new System.Windows.Forms.Label();
        fullNameInput = new System.Windows.Forms.TextBox();
        fullNameLabel.Name = "fullNameLabel"; fullNameLabel.Text = "Full name *";
        fullNameLabel.AutoSize = true; fullNameLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        fullNameInput.Name = "fullName"; fullNameInput.Dock = System.Windows.Forms.DockStyle.Fill;
        fullNameInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(fullNameLabel, 0, 0); fieldsLayout.Controls.Add(fullNameInput, 1, 0);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        contactNumberLabel = new System.Windows.Forms.Label();
        contactNumberInput = new System.Windows.Forms.TextBox();
        contactNumberLabel.Name = "contactNumberLabel"; contactNumberLabel.Text = "Contact number *";
        contactNumberLabel.AutoSize = true; contactNumberLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        contactNumberInput.Name = "contactNumber"; contactNumberInput.Dock = System.Windows.Forms.DockStyle.Fill;
        contactNumberInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(contactNumberLabel, 0, 1); fieldsLayout.Controls.Add(contactNumberInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        emailLabel = new System.Windows.Forms.Label();
        emailInput = new System.Windows.Forms.TextBox();
        emailLabel.Name = "emailLabel"; emailLabel.Text = "Email";
        emailLabel.AutoSize = true; emailLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        emailInput.Name = "email"; emailInput.Dock = System.Windows.Forms.DockStyle.Fill;
        emailInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(emailLabel, 0, 2); fieldsLayout.Controls.Add(emailInput, 1, 2);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        isActiveLabel = new System.Windows.Forms.Label();
        isActiveInput = new System.Windows.Forms.CheckBox();
        isActiveLabel.Name = "isActiveLabel"; isActiveLabel.Text = "Active customer";
        isActiveLabel.AutoSize = true; isActiveLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        isActiveLabel.Visible = false;
        isActiveInput.Name = "isActive"; isActiveInput.Dock = System.Windows.Forms.DockStyle.Fill;
        isActiveInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        isActiveInput.Visible = false;
        fieldsLayout.Controls.Add(isActiveLabel, 0, 3); fieldsLayout.Controls.Add(isActiveInput, 1, 3);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 4;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

