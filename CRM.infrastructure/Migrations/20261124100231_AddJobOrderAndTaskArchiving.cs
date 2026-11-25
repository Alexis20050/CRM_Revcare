using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOrderAndTaskArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "JobTasks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "JobTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "JobTasks",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "JobTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "JobOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "JobOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "JobOrders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "JobOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_JobTasks_IsArchived",
                table: "JobTasks",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_IsArchived",
                table: "JobOrders",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobTasks_IsArchived",
                table: "JobTasks");

            migrationBuilder.DropIndex(
                name: "IX_JobOrders_IsArchived",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "JobOrders");
        }
    }
}
