using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class TenantAWorkshop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Feedbacks",
                newName: "LegacyCustomerId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Inquiries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Unanswered");

            migrationBuilder.AlterColumn<string>(
                name: "InquiryPlatform",
                table: "Inquiries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Inquiries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "ConvertedJobOrderId",
                table: "Inquiries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Inquiries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Inquiries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "VehicleDetails",
                table: "Inquiries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Feedbacks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LegacyCustomerId",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Complaints",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SourceFeedbackId",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UnderWarrantyService",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    AuditEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.AuditEntryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "TenantDatabaseIdentity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDatabaseIdentity", x => x.Id);
                    table.CheckConstraint("CK_TenantIdentity_Singleton", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "JobOrders",
                columns: table => new
                {
                    JobOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehicleDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InquiryNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    EstimateId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOrders", x => x.JobOrderId);
                    table.CheckConstraint("CK_JobOrders_Estimate", "[TotalEstimatedCost] IS NULL OR [TotalEstimatedCost] >= 0");
                    table.ForeignKey(
                        name: "FK_JobOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobTasks",
                columns: table => new
                {
                    JobTaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    JobOrderId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTasks", x => x.JobTaskId);
                    table.ForeignKey(
                        name: "FK_JobTasks_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "JobOrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    VoidReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowCustomerCredit = table.Column<bool>(type: "bit", nullable: false),
                    JobOrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.CheckConstraint("CK_Payments_Positive", "[AmountPaid] > 0");
                    table.ForeignKey(
                        name: "FK_Payments_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "JobOrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_ConvertedJobOrderId",
                table: "Inquiries",
                column: "ConvertedJobOrderId",
                unique: true,
                filter: "[ConvertedJobOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_CustomerId",
                table: "Inquiries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_JobOrderId",
                table: "Feedbacks",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CustomerId",
                table: "Complaints",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_JobOrderId",
                table: "Complaints",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SourceFeedbackId",
                table: "Complaints",
                column: "SourceFeedbackId",
                unique: true,
                filter: "[SourceFeedbackId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ContactNumber",
                table: "Customers",
                column: "ContactNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FullName",
                table: "Customers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_CustomerId",
                table: "JobOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTasks_JobOrderId",
                table: "JobTasks",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_JobOrderId",
                table: "Payments",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubmissionId",
                table: "Payments",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Customers_CustomerId",
                table: "Complaints",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Feedbacks_SourceFeedbackId",
                table: "Complaints",
                column: "SourceFeedbackId",
                principalTable: "Feedbacks",
                principalColumn: "FeedbackId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_JobOrders_JobOrderId",
                table: "Complaints",
                column: "JobOrderId",
                principalTable: "JobOrders",
                principalColumn: "JobOrderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_JobOrders_JobOrderId",
                table: "Feedbacks",
                column: "JobOrderId",
                principalTable: "JobOrders",
                principalColumn: "JobOrderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_Customers_CustomerId",
                table: "Inquiries",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_JobOrders_ConvertedJobOrderId",
                table: "Inquiries",
                column: "ConvertedJobOrderId",
                principalTable: "JobOrders",
                principalColumn: "JobOrderId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Customers_CustomerId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Feedbacks_SourceFeedbackId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_JobOrders_JobOrderId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_JobOrders_JobOrderId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_Customers_CustomerId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_JobOrders_ConvertedJobOrderId",
                table: "Inquiries");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "JobTasks");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "TenantDatabaseIdentity");

            migrationBuilder.DropTable(
                name: "JobOrders");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_ConvertedJobOrderId",
                table: "Inquiries");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_CustomerId",
                table: "Inquiries");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_JobOrderId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_CustomerId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_JobOrderId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_SourceFeedbackId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ConvertedJobOrderId",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "VehicleDetails",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "LegacyCustomerId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "SourceFeedbackId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "UnderWarrantyService",
                table: "Complaints");

            migrationBuilder.RenameColumn(
                name: "LegacyCustomerId",
                table: "Feedbacks",
                newName: "CustomerId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Inquiries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Unanswered",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "InquiryPlatform",
                table: "Inquiries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Inquiries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
