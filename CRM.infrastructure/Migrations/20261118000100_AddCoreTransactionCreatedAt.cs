using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations
{
    public partial class AddCoreTransactionCreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Original creation times are unknown; use the UTC migration time for existing rows.
            migrationBuilder.AddColumn<DateTime>("CreatedAt", "JobOrders", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>("CreatedAt", "JobTasks", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>("CreatedAt", "Payments", type: "datetime2", nullable: true);
            migrationBuilder.Sql(@"
DECLARE @BackfillUtc datetime2 = SYSUTCDATETIME();
UPDATE [JobOrders] SET [CreatedAt] = @BackfillUtc WHERE [CreatedAt] IS NULL;
UPDATE [JobTasks] SET [CreatedAt] = @BackfillUtc WHERE [CreatedAt] IS NULL;
UPDATE [Payments] SET [CreatedAt] = @BackfillUtc WHERE [CreatedAt] IS NULL;");
            migrationBuilder.AlterColumn<DateTime>("CreatedAt", "JobOrders", type: "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
            migrationBuilder.AlterColumn<DateTime>("CreatedAt", "JobTasks", type: "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
            migrationBuilder.AlterColumn<DateTime>("CreatedAt", "Payments", type: "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("CreatedAt", "Payments");
            migrationBuilder.DropColumn("CreatedAt", "JobTasks");
            migrationBuilder.DropColumn("CreatedAt", "JobOrders");
        }
    }
}
