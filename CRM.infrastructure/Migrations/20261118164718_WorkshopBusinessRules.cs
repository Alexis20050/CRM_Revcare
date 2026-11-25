using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkshopBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCustomerCredit",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "JobOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InquiryNotes",
                table: "JobOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEstimatedCost",
                table: "JobOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleDetails",
                table: "JobOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConvertedJobOrderId",
                table: "Inquiries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleDetails",
                table: "Inquiries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Positive",
                table: "Payments",
                sql: "[AmountPaid] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JobOrders_Estimate",
                table: "JobOrders",
                sql: "[TotalEstimatedCost] IS NULL OR [TotalEstimatedCost] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_ConvertedJobOrderId",
                table: "Inquiries",
                column: "ConvertedJobOrderId",
                unique: true,
                filter: "[ConvertedJobOrderId] IS NOT NULL");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Feedbacks_JobOrderId' AND object_id = OBJECT_ID('dbo.Feedbacks')) CREATE INDEX [IX_Feedbacks_JobOrderId] ON [Feedbacks] ([JobOrderId]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Complaints_JobOrderId' AND object_id = OBJECT_ID('dbo.Complaints')) CREATE INDEX [IX_Complaints_JobOrderId] ON [Complaints] ([JobOrderId]);");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SourceFeedbackId",
                table: "Complaints",
                column: "SourceFeedbackId",
                unique: true,
                filter: "[SourceFeedbackId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Feedbacks_SourceFeedbackId",
                table: "Complaints",
                column: "SourceFeedbackId",
                principalTable: "Feedbacks",
                principalColumn: "FeedbackId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Complaints_JobOrders_JobOrderId') ALTER TABLE [Complaints] WITH CHECK ADD CONSTRAINT [FK_Complaints_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId]); ALTER TABLE [Complaints] WITH CHECK CHECK CONSTRAINT [FK_Complaints_JobOrders_JobOrderId];");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Feedbacks_JobOrders_JobOrderId') ALTER TABLE [Feedbacks] WITH CHECK ADD CONSTRAINT [FK_Feedbacks_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId]); ALTER TABLE [Feedbacks] WITH CHECK CHECK CONSTRAINT [FK_Feedbacks_JobOrders_JobOrderId];");

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
            throw new NotSupportedException("Restore a reviewed backup instead of removing financial and warranty history.");
        }
    }
}
