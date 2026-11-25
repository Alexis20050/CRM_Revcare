using System.Windows.Forms;
using System.Drawing;
namespace CRM_Revcare_;
partial class TenantWorkspaceForm
{
    private System.ComponentModel.IContainer components;
    private Label heading, sessionLabel, jobSummary, status;
    private FlowLayoutPanel navigation, actions;
    private DataGridView records, jobsOverview;
    private SplitContainer workspace;
    private TextBox search;
    private ComboBox paymentStatusFilter, billingStatusFilter;
    private CheckBox qaOnly;
    private Button btnSearch, clearSearch, refresh, add, edit, archive, progress, assign, convert, startTask, finishTask, submit, approve, rework, allJobs, previewReceipt, printReceipt, printWorkOrder, viewTasksPopUp, logout, resetPassword, termsAcceptance, checkInAppointment, approveEstimate, convertEstimate;
    private RadioButton customersMenu, inquiriesMenu, appointmentsMenu, ordersMenu, estimatesMenu, tasksMenu, servicesMenu, assignmentsMenu, paymentsMenu, complaintsMenu, feedbackMenu, reportsMenu, staffMenu, staffHistoryMenu, auditMenu;
    private Label customersDivider, inquiriesDivider, appointmentsDivider, ordersDivider, estimatesDivider, tasksDivider, servicesDivider, assignmentsDivider, paymentsDivider, complaintsDivider, feedbackDivider, reportsDivider, staffDivider, staffHistoryDivider, auditDivider;
    private Label headerFrontOffice, headerWorkshop, headerBillingCare, headerAdmin;
    protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        heading = new Label(); sessionLabel = new Label(); jobSummary = new Label(); status = new Label();
        navigation = new FlowLayoutPanel(); actions = new FlowLayoutPanel();
        records = new DataGridView(); jobsOverview = new DataGridView(); workspace = new SplitContainer(); search = new TextBox();
        qaOnly = new CheckBox(); qaOnly.Text = "QA queue"; qaOnly.AutoSize = true; qaOnly.Margin = new Padding(8, 8, 8, 3);
        SuspendLayout();
        heading.Dock = DockStyle.Top; heading.Height = 76; heading.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        heading.Padding = new Padding(14, 16, 14, 16); heading.Text = "REVCARE AUTO HUB CRM - Main Transactions";
        sessionLabel.Dock = DockStyle.Bottom; sessionLabel.Height = 32; sessionLabel.Padding = new Padding(14, 6, 0, 0);
        navigation.Name = "navigation"; navigation.Dock = DockStyle.Left; navigation.Width = 225; navigation.Padding = new Padding(8);
        navigation.FlowDirection = FlowDirection.TopDown; navigation.WrapContents = false; navigation.AutoScroll = true;
        headerFrontOffice = new Label();
        headerFrontOffice.Name = "headerFrontOffice"; headerFrontOffice.Text = "FRONT OFFICE";
        headerFrontOffice.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold); headerFrontOffice.ForeColor = Color.FromArgb(100, 116, 139);
        headerFrontOffice.Size = new Size(190, 20); headerFrontOffice.Margin = new Padding(3, 8, 3, 2); headerFrontOffice.TextAlign = ContentAlignment.BottomLeft;
        navigation.Controls.Add(headerFrontOffice);

        customersMenu = new RadioButton(); customersDivider = new Label();
        customersMenu.Name = "customersMenu"; customersMenu.Appearance = Appearance.Button; customersMenu.Size = new Size(190, 43);
        customersMenu.TextAlign = ContentAlignment.MiddleLeft; customersMenu.Padding = new Padding(8, 0, 0, 0); customersMenu.UseVisualStyleBackColor = true;
        customersDivider.BorderStyle = BorderStyle.Fixed3D; customersDivider.Size = new Size(190, 2); customersDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(customersMenu); navigation.Controls.Add(customersDivider);

        inquiriesMenu = new RadioButton(); inquiriesDivider = new Label();
        inquiriesMenu.Name = "inquiriesMenu"; inquiriesMenu.Appearance = Appearance.Button; inquiriesMenu.Size = new Size(190, 43);
        inquiriesMenu.TextAlign = ContentAlignment.MiddleLeft; inquiriesMenu.Padding = new Padding(8, 0, 0, 0); inquiriesMenu.UseVisualStyleBackColor = true;
        inquiriesDivider.BorderStyle = BorderStyle.Fixed3D; inquiriesDivider.Size = new Size(190, 2); inquiriesDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(inquiriesMenu); navigation.Controls.Add(inquiriesDivider);

        appointmentsMenu = new RadioButton(); appointmentsDivider = new Label();
        appointmentsMenu.Name = "appointmentsMenu"; appointmentsMenu.Appearance = Appearance.Button; appointmentsMenu.Size = new Size(190, 43);
        appointmentsMenu.TextAlign = ContentAlignment.MiddleLeft; appointmentsMenu.Padding = new Padding(8, 0, 0, 0); appointmentsMenu.UseVisualStyleBackColor = true;
        appointmentsDivider.BorderStyle = BorderStyle.Fixed3D; appointmentsDivider.Size = new Size(190, 2); appointmentsDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(appointmentsMenu); navigation.Controls.Add(appointmentsDivider);

        headerWorkshop = new Label();
        headerWorkshop.Name = "headerWorkshop"; headerWorkshop.Text = "WORKSHOP OPERATIONS";
        headerWorkshop.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold); headerWorkshop.ForeColor = Color.FromArgb(100, 116, 139);
        headerWorkshop.Size = new Size(190, 20); headerWorkshop.Margin = new Padding(3, 10, 3, 2); headerWorkshop.TextAlign = ContentAlignment.BottomLeft;
        navigation.Controls.Add(headerWorkshop);

        ordersMenu = new RadioButton(); ordersDivider = new Label();
        ordersMenu.Name = "ordersMenu"; ordersMenu.Appearance = Appearance.Button; ordersMenu.Size = new Size(190, 43);
        ordersMenu.TextAlign = ContentAlignment.MiddleLeft; ordersMenu.Padding = new Padding(8, 0, 0, 0); ordersMenu.UseVisualStyleBackColor = true;
        ordersDivider.BorderStyle = BorderStyle.Fixed3D; ordersDivider.Size = new Size(190, 2); ordersDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(ordersMenu); navigation.Controls.Add(ordersDivider);

        estimatesMenu = new RadioButton(); estimatesDivider = new Label();
        estimatesMenu.Name = "estimatesMenu"; estimatesMenu.Appearance = Appearance.Button; estimatesMenu.Size = new Size(190, 43);
        estimatesMenu.TextAlign = ContentAlignment.MiddleLeft; estimatesMenu.Padding = new Padding(8, 0, 0, 0); estimatesMenu.UseVisualStyleBackColor = true;
        estimatesDivider.BorderStyle = BorderStyle.Fixed3D; estimatesDivider.Size = new Size(190, 2); estimatesDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(estimatesMenu); navigation.Controls.Add(estimatesDivider);

        tasksMenu = new RadioButton(); tasksDivider = new Label();
        tasksMenu.Name = "tasksMenu"; tasksMenu.Appearance = Appearance.Button; tasksMenu.Size = new Size(190, 43);
        tasksMenu.TextAlign = ContentAlignment.MiddleLeft; tasksMenu.Padding = new Padding(8, 0, 0, 0); tasksMenu.UseVisualStyleBackColor = true;
        tasksDivider.BorderStyle = BorderStyle.Fixed3D; tasksDivider.Size = new Size(190, 2); tasksDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(tasksMenu); navigation.Controls.Add(tasksDivider);

        servicesMenu = new RadioButton(); servicesDivider = new Label();
        servicesMenu.Name = "servicesMenu"; servicesMenu.Appearance = Appearance.Button; servicesMenu.Size = new Size(190, 43);
        servicesMenu.TextAlign = ContentAlignment.MiddleLeft; servicesMenu.Padding = new Padding(8, 0, 0, 0); servicesMenu.UseVisualStyleBackColor = true;
        servicesDivider.BorderStyle = BorderStyle.Fixed3D; servicesDivider.Size = new Size(190, 2); servicesDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(servicesMenu); navigation.Controls.Add(servicesDivider);

        assignmentsMenu = new RadioButton(); assignmentsDivider = new Label();
        assignmentsMenu.Name = "assignmentsMenu"; assignmentsMenu.Appearance = Appearance.Button; assignmentsMenu.Size = new Size(190, 43);
        assignmentsMenu.TextAlign = ContentAlignment.MiddleLeft; assignmentsMenu.Padding = new Padding(8, 0, 0, 0); assignmentsMenu.UseVisualStyleBackColor = true;
        assignmentsDivider.BorderStyle = BorderStyle.Fixed3D; assignmentsDivider.Size = new Size(190, 2); assignmentsDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(assignmentsMenu); navigation.Controls.Add(assignmentsDivider);

        headerBillingCare = new Label();
        headerBillingCare.Name = "headerBillingCare"; headerBillingCare.Text = "BILLING & AFTER-SALES";
        headerBillingCare.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold); headerBillingCare.ForeColor = Color.FromArgb(100, 116, 139);
        headerBillingCare.Size = new Size(190, 20); headerBillingCare.Margin = new Padding(3, 10, 3, 2); headerBillingCare.TextAlign = ContentAlignment.BottomLeft;
        navigation.Controls.Add(headerBillingCare);

        paymentsMenu = new RadioButton(); paymentsDivider = new Label();
        paymentsMenu.Name = "paymentsMenu"; paymentsMenu.Appearance = Appearance.Button; paymentsMenu.Size = new Size(190, 43);
        paymentsMenu.TextAlign = ContentAlignment.MiddleLeft; paymentsMenu.Padding = new Padding(8, 0, 0, 0); paymentsMenu.UseVisualStyleBackColor = true;
        paymentsDivider.BorderStyle = BorderStyle.Fixed3D; paymentsDivider.Size = new Size(190, 2); paymentsDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(paymentsMenu); navigation.Controls.Add(paymentsDivider);

        complaintsMenu = new RadioButton(); complaintsDivider = new Label();
        complaintsMenu.Name = "complaintsMenu"; complaintsMenu.Appearance = Appearance.Button; complaintsMenu.Size = new Size(190, 43);
        complaintsMenu.TextAlign = ContentAlignment.MiddleLeft; complaintsMenu.Padding = new Padding(8, 0, 0, 0); complaintsMenu.UseVisualStyleBackColor = true;
        complaintsDivider.BorderStyle = BorderStyle.Fixed3D; complaintsDivider.Size = new Size(190, 2); complaintsDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(complaintsMenu); navigation.Controls.Add(complaintsDivider);

        feedbackMenu = new RadioButton(); feedbackDivider = new Label();
        feedbackMenu.Name = "feedbackMenu"; feedbackMenu.Appearance = Appearance.Button; feedbackMenu.Size = new Size(190, 43);
        feedbackMenu.TextAlign = ContentAlignment.MiddleLeft; feedbackMenu.Padding = new Padding(8, 0, 0, 0); feedbackMenu.UseVisualStyleBackColor = true;
        feedbackDivider.BorderStyle = BorderStyle.Fixed3D; feedbackDivider.Size = new Size(190, 2); feedbackDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(feedbackMenu); navigation.Controls.Add(feedbackDivider);

        reportsMenu = new RadioButton(); reportsDivider = new Label();
        reportsMenu.Name = "reportsMenu"; reportsMenu.Appearance = Appearance.Button; reportsMenu.Size = new Size(190, 43);
        reportsMenu.TextAlign = ContentAlignment.MiddleLeft; reportsMenu.Padding = new Padding(8, 0, 0, 0); reportsMenu.UseVisualStyleBackColor = true;
        reportsDivider.BorderStyle = BorderStyle.Fixed3D; reportsDivider.Size = new Size(190, 2); reportsDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(reportsMenu); navigation.Controls.Add(reportsDivider);

        headerAdmin = new Label();
        headerAdmin.Name = "headerAdmin"; headerAdmin.Text = "MANAGEMENT & AUDIT";
        headerAdmin.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold); headerAdmin.ForeColor = Color.FromArgb(100, 116, 139);
        headerAdmin.Size = new Size(190, 20); headerAdmin.Margin = new Padding(3, 10, 3, 2); headerAdmin.TextAlign = ContentAlignment.BottomLeft;
        navigation.Controls.Add(headerAdmin);

        staffMenu = new RadioButton(); staffDivider = new Label();
        staffMenu.Name = "staffMenu"; staffMenu.Appearance = Appearance.Button; staffMenu.Size = new Size(190, 43);
        staffMenu.TextAlign = ContentAlignment.MiddleLeft; staffMenu.Padding = new Padding(8, 0, 0, 0); staffMenu.UseVisualStyleBackColor = true;
        staffDivider.BorderStyle = BorderStyle.Fixed3D; staffDivider.Size = new Size(190, 2); staffDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(staffMenu); navigation.Controls.Add(staffDivider);

        staffHistoryMenu = new RadioButton(); staffHistoryDivider = new Label();
        staffHistoryMenu.Name = "staffHistoryMenu"; staffHistoryMenu.Appearance = Appearance.Button; staffHistoryMenu.Size = new Size(190, 43);
        staffHistoryMenu.TextAlign = ContentAlignment.MiddleLeft; staffHistoryMenu.Padding = new Padding(8, 0, 0, 0); staffHistoryMenu.UseVisualStyleBackColor = true;
        staffHistoryDivider.BorderStyle = BorderStyle.Fixed3D; staffHistoryDivider.Size = new Size(190, 2); staffHistoryDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(staffHistoryMenu); navigation.Controls.Add(staffHistoryDivider);

        auditMenu = new RadioButton(); auditDivider = new Label();
        auditMenu.Name = "auditMenu"; auditMenu.Appearance = Appearance.Button; auditMenu.Size = new Size(190, 43);
        auditMenu.TextAlign = ContentAlignment.MiddleLeft; auditMenu.Padding = new Padding(8, 0, 0, 0); auditMenu.UseVisualStyleBackColor = true;
        auditDivider.BorderStyle = BorderStyle.Fixed3D; auditDivider.Size = new Size(190, 2); auditDivider.Margin = new Padding(3, 6, 3, 6);
        navigation.Controls.Add(auditMenu); navigation.Controls.Add(auditDivider);
        actions.Dock = DockStyle.Top; actions.AutoSize = true; actions.Padding = new Padding(8); actions.MinimumSize = new Size(0, 90);
        search.Width = 260; search.PlaceholderText = "Search customer, job, vehicle or service...";
        btnSearch = new Button(); btnSearch.Text = "Search"; btnSearch.AutoSize = true; btnSearch.MinimumSize = new Size(70, 34); btnSearch.UseVisualStyleBackColor = true;
        clearSearch = new Button(); clearSearch.Text = "Clear"; clearSearch.AutoSize = true; clearSearch.MinimumSize = new Size(60, 34); clearSearch.UseVisualStyleBackColor = true;
        paymentStatusFilter = new ComboBox(); paymentStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList; paymentStatusFilter.Width = 120;
        paymentStatusFilter.Items.AddRange(new object[] { "All Payments", "Valid Only", "Voided Only" }); paymentStatusFilter.SelectedIndex = 0; paymentStatusFilter.Visible = false;
        billingStatusFilter = new ComboBox(); billingStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList; billingStatusFilter.Width = 135;
        billingStatusFilter.Items.AddRange(new object[] { "All Billing Statuses", "Unpaid", "Partially Paid", "Fully Paid" }); billingStatusFilter.SelectedIndex = 0; billingStatusFilter.Visible = false;
        refresh = new Button(); refresh.Text = "Refresh"; refresh.AutoSize = true; refresh.MinimumSize = new Size(80, 34); refresh.UseVisualStyleBackColor = true;
        add = new Button(); add.Text = "Add"; add.AutoSize = true; add.MinimumSize = new Size(80, 34); add.UseVisualStyleBackColor = true;
        edit = new Button(); edit.Text = "Edit"; edit.AutoSize = true; edit.MinimumSize = new Size(80, 34); edit.UseVisualStyleBackColor = true;
        archive = new Button(); archive.Text = "Archive"; archive.AutoSize = true; archive.MinimumSize = new Size(80, 34); archive.UseVisualStyleBackColor = true;
        progress = new Button(); progress.Text = "Update job"; progress.AutoSize = true; progress.MinimumSize = new Size(80, 34); progress.UseVisualStyleBackColor = true;
        assign = new Button(); assign.Text = "Assign task"; assign.AutoSize = true; assign.MinimumSize = new Size(80, 34); assign.UseVisualStyleBackColor = true;
        convert = new Button(); convert.Text = "Convert to job"; convert.AutoSize = true; convert.MinimumSize = new Size(80, 34); convert.UseVisualStyleBackColor = true;
        startTask = new Button(); startTask.Text = "Start task"; startTask.AutoSize = true; startTask.MinimumSize = new Size(80, 34); startTask.UseVisualStyleBackColor = true;
        finishTask = new Button(); finishTask.Text = "Mark task done"; finishTask.AutoSize = true; finishTask.MinimumSize = new Size(80, 34); finishTask.UseVisualStyleBackColor = true;
        submit = new Button(); submit.Text = "Submit for review"; submit.AutoSize = true; submit.MinimumSize = new Size(80, 34); submit.UseVisualStyleBackColor = true;
        approve = new Button(); approve.Text = "Approve QA"; approve.AutoSize = true; approve.MinimumSize = new Size(80, 34); approve.UseVisualStyleBackColor = true;
        rework = new Button(); rework.Text = "Return for rework"; rework.AutoSize = true; rework.MinimumSize = new Size(80, 34); rework.UseVisualStyleBackColor = true;
        allJobs = new Button(); allJobs.Text = "All jobs"; allJobs.AutoSize = true; allJobs.MinimumSize = new Size(80, 34); allJobs.UseVisualStyleBackColor = true;
        previewReceipt = new Button(); previewReceipt.Text = "👁 Receipt"; previewReceipt.AutoSize = true; previewReceipt.MinimumSize = new Size(80, 34); previewReceipt.UseVisualStyleBackColor = true; previewReceipt.Visible = false;
        printReceipt = new Button(); printReceipt.Text = "🖨 Print Receipt"; printReceipt.AutoSize = true; printReceipt.MinimumSize = new Size(100, 34); printReceipt.UseVisualStyleBackColor = true; printReceipt.Visible = false;
        printWorkOrder = new Button(); printWorkOrder.Text = "📋 Work Order Copy"; printWorkOrder.AutoSize = true; printWorkOrder.MinimumSize = new Size(130, 34); printWorkOrder.UseVisualStyleBackColor = true; printWorkOrder.Visible = false;
        viewTasksPopUp = new Button(); viewTasksPopUp.Text = "📋 View Tasks (Pop-up)"; viewTasksPopUp.AutoSize = true; viewTasksPopUp.MinimumSize = new Size(130, 34); viewTasksPopUp.UseVisualStyleBackColor = true; viewTasksPopUp.Visible = false;
        checkInAppointment = new Button(); checkInAppointment.Text = "Check In"; checkInAppointment.AutoSize = true; checkInAppointment.MinimumSize = new Size(80, 34); checkInAppointment.UseVisualStyleBackColor = true; checkInAppointment.Visible = false;
        approveEstimate = new Button(); approveEstimate.Text = "Approve Estimate"; approveEstimate.AutoSize = true; approveEstimate.MinimumSize = new Size(130, 34); approveEstimate.UseVisualStyleBackColor = true; approveEstimate.Visible = false;
        convertEstimate = new Button(); convertEstimate.Text = "Convert to Job"; convertEstimate.AutoSize = true; convertEstimate.MinimumSize = new Size(120, 34); convertEstimate.UseVisualStyleBackColor = true; convertEstimate.Visible = false;
        termsAcceptance = new Button(); termsAcceptance.Text = "📋 Accept Terms"; termsAcceptance.AutoSize = true; termsAcceptance.MinimumSize = new Size(120, 34); termsAcceptance.UseVisualStyleBackColor = true; termsAcceptance.Visible = false;
        resetPassword = new Button(); resetPassword.Text = "🔑 Reset Password"; resetPassword.AutoSize = true; resetPassword.MinimumSize = new Size(130, 34); resetPassword.UseVisualStyleBackColor = true; resetPassword.Visible = false;
        logout = new Button(); logout.Text = "Sign out"; logout.AutoSize = true; logout.MinimumSize = new Size(80, 34); logout.UseVisualStyleBackColor = true;
        actions.Controls.AddRange(new Control[] { search, btnSearch, clearSearch, paymentStatusFilter, billingStatusFilter, qaOnly, refresh, add, edit, archive, progress, assign, convert, checkInAppointment, approveEstimate, convertEstimate, termsAcceptance, resetPassword, startTask, finishTask, submit, approve, rework, allJobs, viewTasksPopUp, previewReceipt, printReceipt, printWorkOrder, logout });
        workspace.Dock = DockStyle.Fill; workspace.Orientation = Orientation.Horizontal;
        workspace.Panel1Collapsed = true;
        workspace.Size = new Size(1000, 580); workspace.SplitterDistance = 220; workspace.Panel1MinSize = 90; workspace.Panel2MinSize = 150;
        workspace.Panel1.Controls.Add(jobsOverview); workspace.Panel2.Controls.Add(records); workspace.Panel2.Controls.Add(jobSummary);
        jobsOverview.Dock = DockStyle.Fill; records.Dock = DockStyle.Fill;
        jobSummary.Dock = DockStyle.Top; jobSummary.Height = 38; jobSummary.Padding = new Padding(8); jobSummary.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        records.ReadOnly = true; records.AllowUserToAddRows = false; records.AllowUserToDeleteRows = false; records.MultiSelect = false; records.TabStop = false;
        records.SelectionMode = DataGridViewSelectionMode.FullRowSelect; records.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        records.BackgroundColor = Color.White; records.RowHeadersVisible = false;
        records.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 250); records.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        records.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; records.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        records.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing; records.ColumnHeadersHeight = 46;
        jobsOverview.ReadOnly = true; jobsOverview.AllowUserToAddRows = false; jobsOverview.AllowUserToDeleteRows = false; jobsOverview.MultiSelect = false; jobsOverview.TabStop = false;
        jobsOverview.SelectionMode = DataGridViewSelectionMode.FullRowSelect; jobsOverview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        jobsOverview.BackgroundColor = Color.White; jobsOverview.RowHeadersVisible = false;
        jobsOverview.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 250); jobsOverview.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        jobsOverview.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; jobsOverview.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        jobsOverview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing; jobsOverview.ColumnHeadersHeight = 46;
        status.Dock = DockStyle.Bottom; status.Height = 34; status.Padding = new Padding(8);
        Controls.Add(workspace); Controls.Add(actions); Controls.Add(status); Controls.Add(navigation); Controls.Add(sessionLabel); Controls.Add(heading);
        AutoScaleMode = AutoScaleMode.Font; Font = new Font("Segoe UI", 10F); ClientSize = new Size(1340, 850); MinimumSize = new Size(1080, 720);
        Text = "RevCare Auto Hub CRM - Tenant A"; StartPosition = FormStartPosition.CenterScreen;
        ResumeLayout(false); PerformLayout();
    }
}
