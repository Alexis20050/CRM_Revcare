namespace CRM_Revcare_;
partial class TenantServiceForm
{
    private System.Windows.Forms.Label nameLabel;
    private System.Windows.Forms.TextBox nameInput;
    private System.Windows.Forms.Label priceLabel;
    private System.Windows.Forms.TextBox priceInput;
    private System.Windows.Forms.Label isActiveLabel;
    private System.Windows.Forms.CheckBox isActiveInput;
    private void InitializeComponent()
    {
        fieldsLayout.SuspendLayout();
        SuspendLayout();
        nameLabel = new System.Windows.Forms.Label();
        nameInput = new System.Windows.Forms.TextBox();
        nameLabel.Name = "nameLabel"; nameLabel.Text = "Service name *";
        nameLabel.AutoSize = true; nameLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        nameInput.Name = "name"; nameInput.Dock = System.Windows.Forms.DockStyle.Fill;
        nameInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(nameLabel, 0, 0); fieldsLayout.Controls.Add(nameInput, 1, 0);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        priceLabel = new System.Windows.Forms.Label();
        priceInput = new System.Windows.Forms.TextBox();
        priceLabel.Name = "priceLabel"; priceLabel.Text = "Reference price *";
        priceLabel.AutoSize = true; priceLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        priceInput.Name = "price"; priceInput.Dock = System.Windows.Forms.DockStyle.Fill;
        priceInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(priceLabel, 0, 1); fieldsLayout.Controls.Add(priceInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        isActiveLabel = new System.Windows.Forms.Label();
        isActiveInput = new System.Windows.Forms.CheckBox();
        isActiveLabel.Name = "isActiveLabel"; isActiveLabel.Text = "Available for new jobs";
        isActiveLabel.AutoSize = true; isActiveLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        isActiveInput.Name = "isActive"; isActiveInput.Dock = System.Windows.Forms.DockStyle.Fill;
        isActiveInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(isActiveLabel, 0, 2); fieldsLayout.Controls.Add(isActiveInput, 1, 2);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 3;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

