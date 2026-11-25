using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialKeyToCompanyDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyDatabases_Companies_CompanyId1",
                table: "CompanyDatabases");

            migrationBuilder.DropIndex(
                name: "IX_CompanyDatabases_CompanyId1",
                table: "CompanyDatabases");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "CompanyDatabases");

            migrationBuilder.AddColumn<string>(
                name: "CredentialKey",
                table: "CompanyDatabases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredentialKey",
                table: "CompanyDatabases");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "CompanyDatabases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDatabases_CompanyId1",
                table: "CompanyDatabases",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyDatabases_Companies_CompanyId1",
                table: "CompanyDatabases",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "CompanyId");
        }
    }
}
