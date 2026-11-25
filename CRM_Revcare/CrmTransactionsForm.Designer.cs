using System.Windows.Forms;
namespace CRM_Revcare_
{
    partial class CrmTransactionsForm
    {
        private System.ComponentModel.IContainer components;
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            customersPage = new CustomerPage();
            customerMenu = new RadioButton();
            customerMenuDivider = new Label();
            header = new Label();
            content = new Panel();
            inquiriesPage = new InquiryPage();
            complaintsPage = new ComplaintPage();
            feedbackPage = new FeedbackPage();
            ordersPage = new JobOrderPage();
            tasksPage = new JobTaskPage();
            paymentsPage = new PaymentPage();
            sidebar = new Panel();
            menuLayout = new FlowLayoutPanel();
            inquiryMenu = new RadioButton();
            inquiryMenuDivider = new Label();
            complaintMenu = new RadioButton();
            complaintMenuDivider = new Label();
            feedbackMenu = new RadioButton();
            feedbackMenuDivider = new Label();
            orderMenu = new RadioButton();
            orderMenuDivider = new Label();
            taskMenu = new RadioButton();
            taskMenuDivider = new Label();
            paymentMenu = new RadioButton();
            paymentMenuDivider = new Label();
            menuLabel = new Label();
            sessionBar = new FlowLayoutPanel();
            syncButton = new Button();
            logoutButton = new Button();
            syncStatus = new Label();
            refreshTimer = new Timer(components);
            content.SuspendLayout();
            sidebar.SuspendLayout();
            menuLayout.SuspendLayout();
            sessionBar.SuspendLayout();
            SuspendLayout();
            // 
            // customersPage
            // 
            customersPage.Dock = DockStyle.Fill;
            customersPage.Location = new System.Drawing.Point(0, 0);
            customersPage.Margin = new Padding(3, 4, 3, 4);
            customersPage.Name = "customersPage";
            customersPage.Size = new System.Drawing.Size(1337, 873);
            customersPage.TabIndex = 6;
            customersPage.Visible = false;
            // 
            // customerMenu
            // 
            customerMenu.Appearance = Appearance.Button;
            customerMenu.Location = new System.Drawing.Point(3, 484);
            customerMenu.Margin = new Padding(3, 4, 3, 4);
            customerMenu.Name = "customerMenu";
            customerMenu.Size = new System.Drawing.Size(210, 53);
            customerMenu.TabIndex = 7;
            customerMenu.Text = "Customers";
            // 
            // customerMenuDivider
            // 
            customerMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            customerMenuDivider.Location = new System.Drawing.Point(3, 549);
            customerMenuDivider.Margin = new Padding(3, 8, 3, 8);
            customerMenuDivider.Name = "customerMenuDivider";
            customerMenuDivider.Size = new System.Drawing.Size(210, 3);
            customerMenuDivider.TabIndex = 8;
            // 
            // header
            // 
            header.Dock = DockStyle.Top;
            header.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            header.Location = new System.Drawing.Point(0, 0);
            header.Name = "header";
            header.Padding = new Padding(14, 16, 14, 16);
            header.Size = new System.Drawing.Size(1577, 80);
            header.TabIndex = 2;
            header.Text = "REVCARE AUTO HUB CRM - Main Transactions";
            header.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // content
            // 
            content.Controls.Add(inquiriesPage);
            content.Controls.Add(complaintsPage);
            content.Controls.Add(feedbackPage);
            content.Controls.Add(ordersPage);
            content.Controls.Add(tasksPage);
            content.Controls.Add(paymentsPage);
            content.Controls.Add(customersPage);
            content.Dock = DockStyle.Fill;
            content.Location = new System.Drawing.Point(240, 80);
            content.Margin = new Padding(3, 4, 3, 4);
            content.Name = "content";
            content.Size = new System.Drawing.Size(1337, 873);
            content.TabIndex = 0;
            // 
            // inquiriesPage
            // 
            inquiriesPage.Dock = DockStyle.Fill;
            inquiriesPage.Location = new System.Drawing.Point(0, 0);
            inquiriesPage.Margin = new Padding(3, 5, 3, 5);
            inquiriesPage.Name = "inquiriesPage";
            inquiriesPage.Padding = new Padding(10, 15, 10, 15);
            inquiriesPage.Size = new System.Drawing.Size(1337, 873);
            inquiriesPage.TabIndex = 0;
            inquiriesPage.Load += inquiriesPage_Load;
            // 
            // complaintsPage
            // 
            complaintsPage.Dock = DockStyle.Fill;
            complaintsPage.Location = new System.Drawing.Point(0, 0);
            complaintsPage.Margin = new Padding(3, 5, 3, 5);
            complaintsPage.Name = "complaintsPage";
            complaintsPage.Padding = new Padding(10, 15, 10, 15);
            complaintsPage.Size = new System.Drawing.Size(1337, 873);
            complaintsPage.TabIndex = 1;
            complaintsPage.Visible = false;
            // 
            // feedbackPage
            // 
            feedbackPage.Dock = DockStyle.Fill;
            feedbackPage.Location = new System.Drawing.Point(0, 0);
            feedbackPage.Margin = new Padding(3, 5, 3, 5);
            feedbackPage.Name = "feedbackPage";
            feedbackPage.Padding = new Padding(10, 15, 10, 15);
            feedbackPage.Size = new System.Drawing.Size(1337, 873);
            feedbackPage.TabIndex = 2;
            feedbackPage.Visible = false;
            // 
            // ordersPage
            // 
            ordersPage.Dock = DockStyle.Fill;
            ordersPage.Location = new System.Drawing.Point(0, 0);
            ordersPage.Margin = new Padding(3, 5, 3, 5);
            ordersPage.Name = "ordersPage";
            ordersPage.Padding = new Padding(10, 15, 10, 15);
            ordersPage.Size = new System.Drawing.Size(1337, 873);
            ordersPage.TabIndex = 3;
            ordersPage.Visible = false;
            // 
            // tasksPage
            // 
            tasksPage.Dock = DockStyle.Fill;
            tasksPage.Location = new System.Drawing.Point(0, 0);
            tasksPage.Margin = new Padding(3, 5, 3, 5);
            tasksPage.Name = "tasksPage";
            tasksPage.Padding = new Padding(10, 15, 10, 15);
            tasksPage.Size = new System.Drawing.Size(1337, 873);
            tasksPage.TabIndex = 4;
            tasksPage.Visible = false;
            // 
            // paymentsPage
            // 
            paymentsPage.Dock = DockStyle.Fill;
            paymentsPage.Location = new System.Drawing.Point(0, 0);
            paymentsPage.Margin = new Padding(3, 5, 3, 5);
            paymentsPage.Name = "paymentsPage";
            paymentsPage.Padding = new Padding(10, 15, 10, 15);
            paymentsPage.Size = new System.Drawing.Size(1337, 873);
            paymentsPage.TabIndex = 5;
            paymentsPage.Visible = false;
            // 
            // sidebar
            // 
            sidebar.Controls.Add(menuLayout);
            sidebar.Controls.Add(menuLabel);
            sidebar.Dock = DockStyle.Left;
            sidebar.Location = new System.Drawing.Point(0, 80);
            sidebar.Margin = new Padding(3, 4, 3, 4);
            sidebar.Name = "sidebar";
            sidebar.Padding = new Padding(9, 11, 9, 11);
            sidebar.Size = new System.Drawing.Size(240, 873);
            sidebar.TabIndex = 1;
            // 
            // menuLayout
            // 
            menuLayout.AutoScroll = true;
            menuLayout.Controls.Add(customerMenu);
            menuLayout.Controls.Add(customerMenuDivider);
            menuLayout.Controls.Add(inquiryMenu);
            menuLayout.Controls.Add(inquiryMenuDivider);
            menuLayout.Controls.Add(complaintMenu);
            menuLayout.Controls.Add(complaintMenuDivider);
            menuLayout.Controls.Add(feedbackMenu);
            menuLayout.Controls.Add(feedbackMenuDivider);
            menuLayout.Controls.Add(orderMenu);
            menuLayout.Controls.Add(orderMenuDivider);
            menuLayout.Controls.Add(taskMenu);
            menuLayout.Controls.Add(taskMenuDivider);
            menuLayout.Controls.Add(paymentMenu);
            menuLayout.Controls.Add(paymentMenuDivider);
            menuLayout.Dock = DockStyle.Fill;
            menuLayout.FlowDirection = FlowDirection.TopDown;
            menuLayout.Location = new System.Drawing.Point(9, 58);
            menuLayout.Margin = new Padding(3, 4, 3, 4);
            menuLayout.Name = "menuLayout";
            menuLayout.Size = new System.Drawing.Size(222, 804);
            menuLayout.TabIndex = 0;
            menuLayout.WrapContents = false;
            // 
            // inquiryMenu
            // 
            inquiryMenu.Appearance = Appearance.Button;
            inquiryMenu.Location = new System.Drawing.Point(3, 4);
            inquiryMenu.Margin = new Padding(3, 4, 3, 4);
            inquiryMenu.Name = "inquiryMenu";
            inquiryMenu.Size = new System.Drawing.Size(210, 53);
            inquiryMenu.TabIndex = 0;
            inquiryMenu.Text = "Inquiry";
            // 
            // inquiryMenuDivider
            // 
            inquiryMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            inquiryMenuDivider.Location = new System.Drawing.Point(3, 69);
            inquiryMenuDivider.Margin = new Padding(3, 8, 3, 8);
            inquiryMenuDivider.Name = "inquiryMenuDivider";
            inquiryMenuDivider.Size = new System.Drawing.Size(210, 3);
            inquiryMenuDivider.TabIndex = 1;
            // 
            // complaintMenu
            // 
            complaintMenu.Appearance = Appearance.Button;
            complaintMenu.Location = new System.Drawing.Point(3, 84);
            complaintMenu.Margin = new Padding(3, 4, 3, 4);
            complaintMenu.Name = "complaintMenu";
            complaintMenu.Size = new System.Drawing.Size(210, 53);
            complaintMenu.TabIndex = 1;
            complaintMenu.Text = "Complaints";
            // 
            // complaintMenuDivider
            // 
            complaintMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            complaintMenuDivider.Location = new System.Drawing.Point(3, 149);
            complaintMenuDivider.Margin = new Padding(3, 8, 3, 8);
            complaintMenuDivider.Name = "complaintMenuDivider";
            complaintMenuDivider.Size = new System.Drawing.Size(210, 3);
            complaintMenuDivider.TabIndex = 2;
            // 
            // feedbackMenu
            // 
            feedbackMenu.Appearance = Appearance.Button;
            feedbackMenu.Location = new System.Drawing.Point(3, 164);
            feedbackMenu.Margin = new Padding(3, 4, 3, 4);
            feedbackMenu.Name = "feedbackMenu";
            feedbackMenu.Size = new System.Drawing.Size(210, 53);
            feedbackMenu.TabIndex = 2;
            feedbackMenu.Text = "Feedback";
            // 
            // feedbackMenuDivider
            // 
            feedbackMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            feedbackMenuDivider.Location = new System.Drawing.Point(3, 229);
            feedbackMenuDivider.Margin = new Padding(3, 8, 3, 8);
            feedbackMenuDivider.Name = "feedbackMenuDivider";
            feedbackMenuDivider.Size = new System.Drawing.Size(210, 3);
            feedbackMenuDivider.TabIndex = 3;
            // 
            // orderMenu
            // 
            orderMenu.Appearance = Appearance.Button;
            orderMenu.Location = new System.Drawing.Point(3, 244);
            orderMenu.Margin = new Padding(3, 4, 3, 4);
            orderMenu.Name = "orderMenu";
            orderMenu.Size = new System.Drawing.Size(210, 53);
            orderMenu.TabIndex = 3;
            orderMenu.Text = "Job Orders";
            // 
            // orderMenuDivider
            // 
            orderMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            orderMenuDivider.Location = new System.Drawing.Point(3, 309);
            orderMenuDivider.Margin = new Padding(3, 8, 3, 8);
            orderMenuDivider.Name = "orderMenuDivider";
            orderMenuDivider.Size = new System.Drawing.Size(210, 3);
            orderMenuDivider.TabIndex = 4;
            // 
            // taskMenu
            // 
            taskMenu.Appearance = Appearance.Button;
            taskMenu.Location = new System.Drawing.Point(3, 324);
            taskMenu.Margin = new Padding(3, 4, 3, 4);
            taskMenu.Name = "taskMenu";
            taskMenu.Size = new System.Drawing.Size(210, 53);
            taskMenu.TabIndex = 4;
            taskMenu.Text = "Job Tasks (Services)";
            // 
            // taskMenuDivider
            // 
            taskMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            taskMenuDivider.Location = new System.Drawing.Point(3, 389);
            taskMenuDivider.Margin = new Padding(3, 8, 3, 8);
            taskMenuDivider.Name = "taskMenuDivider";
            taskMenuDivider.Size = new System.Drawing.Size(210, 3);
            taskMenuDivider.TabIndex = 5;
            // 
            // paymentMenu
            // 
            paymentMenu.Appearance = Appearance.Button;
            paymentMenu.Location = new System.Drawing.Point(3, 404);
            paymentMenu.Margin = new Padding(3, 4, 3, 4);
            paymentMenu.Name = "paymentMenu";
            paymentMenu.Size = new System.Drawing.Size(210, 53);
            paymentMenu.TabIndex = 5;
            paymentMenu.Text = "Payments (Billing)";
            // 
            // paymentMenuDivider
            // 
            paymentMenuDivider.BorderStyle = BorderStyle.Fixed3D;
            paymentMenuDivider.Location = new System.Drawing.Point(3, 469);
            paymentMenuDivider.Margin = new Padding(3, 8, 3, 8);
            paymentMenuDivider.Name = "paymentMenuDivider";
            paymentMenuDivider.Size = new System.Drawing.Size(210, 3);
            paymentMenuDivider.TabIndex = 6;
            // 
            // menuLabel
            // 
            menuLabel.Dock = DockStyle.Top;
            menuLabel.Location = new System.Drawing.Point(9, 11);
            menuLabel.Name = "menuLabel";
            menuLabel.Size = new System.Drawing.Size(222, 47);
            menuLabel.TabIndex = 1;
            menuLabel.Text = "MAIN TRANSACTIONS";
            // 
            // sessionBar
            // 
            sessionBar.Controls.Add(syncButton);
            sessionBar.Controls.Add(logoutButton);
            sessionBar.Controls.Add(syncStatus);
            sessionBar.Dock = DockStyle.Bottom;
            sessionBar.Location = new System.Drawing.Point(0, 953);
            sessionBar.Margin = new Padding(3, 4, 3, 4);
            sessionBar.Name = "sessionBar";
            sessionBar.Padding = new Padding(6, 7, 6, 7);
            sessionBar.Size = new System.Drawing.Size(1577, 60);
            sessionBar.TabIndex = 3;
            // 
            // syncButton
            // 
            syncButton.Location = new System.Drawing.Point(9, 11);
            syncButton.Margin = new Padding(3, 4, 3, 4);
            syncButton.Name = "syncButton";
            syncButton.Size = new System.Drawing.Size(131, 40);
            syncButton.TabIndex = 0;
            syncButton.Text = "Refresh Cloud";
            // 
            // logoutButton
            // 
            logoutButton.Location = new System.Drawing.Point(146, 11);
            logoutButton.Margin = new Padding(3, 4, 3, 4);
            logoutButton.Name = "logoutButton";
            logoutButton.Size = new System.Drawing.Size(97, 40);
            logoutButton.TabIndex = 1;
            logoutButton.Text = "Logout";
            // 
            // syncStatus
            // 
            syncStatus.AutoSize = true;
            syncStatus.Location = new System.Drawing.Point(257, 18);
            syncStatus.Margin = new Padding(11, 11, 3, 4);
            syncStatus.Name = "syncStatus";
            syncStatus.Size = new System.Drawing.Size(297, 20);
            syncStatus.TabIndex = 2;
            syncStatus.Text = "Signed in as admin. Waiting for first refresh.";
            // 
            // refreshTimer
            // 
            refreshTimer.Interval = 60000;
            // 
            // CrmTransactionsForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1280, 720);
            Controls.Add(content);
            Controls.Add(sidebar);
            Controls.Add(header);
            Controls.Add(sessionBar);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new System.Drawing.Size(1024, 600);
            Name = "CrmTransactionsForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "RevCare Auto Hub CRM - Main Transactions";
            content.ResumeLayout(false);
            sidebar.ResumeLayout(false);
            menuLayout.ResumeLayout(false);
            sessionBar.ResumeLayout(false);
            sessionBar.PerformLayout();
            ResumeLayout(false);
        }
        private CustomerPage customersPage;
        private RadioButton customerMenu;
        private Label customerMenuDivider;
        private Label header;
        private Panel content;
        private Panel sidebar;
        private FlowLayoutPanel menuLayout;
        private Label menuLabel;
        private FlowLayoutPanel sessionBar;
        private Button syncButton;
        private Button logoutButton;
        private Label syncStatus;
        private InquiryPage inquiriesPage;
        private ComplaintPage complaintsPage;
        private FeedbackPage feedbackPage;
        private JobOrderPage ordersPage;
        private JobTaskPage tasksPage;
        private PaymentPage paymentsPage;
        private RadioButton inquiryMenu;
        private RadioButton complaintMenu;
        private RadioButton feedbackMenu;
        private RadioButton orderMenu;
        private RadioButton taskMenu;
        private RadioButton paymentMenu;
        private Label inquiryMenuDivider;
        private Label complaintMenuDivider;
        private Label feedbackMenuDivider;
        private Label orderMenuDivider;
        private Label taskMenuDivider;
        private Label paymentMenuDivider;
        private System.Windows.Forms.Timer refreshTimer;
    }
}
