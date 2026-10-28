using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAdminFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Account",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Account] SET [IsAdmin] = CASE WHEN [Role] = N'Admin' THEN 1 ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Account",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Claimant");

            migrationBuilder.Sql(
                "UPDATE [Account] SET [Role] = CASE WHEN [IsAdmin] = 1 THEN N'Admin' ELSE N'Claimant' END;");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Account");
        }
    }
}
