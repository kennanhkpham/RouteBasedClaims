
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    public partial class RenameAccountJobOccupationToDepartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // If both columns exist, retain Department and recover any values
            // that only exist in the old JobOccupation column.
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NOT NULL " +
                "AND COL_LENGTH(N'dbo.Account', N'Department') IS NOT NULL " +
                "BEGIN " +
                "UPDATE [dbo].[Account] SET [Department] = [JobOccupation] " +
                "WHERE ([Department] IS NULL OR [Department] = N'') AND [JobOccupation] IS NOT NULL; " +
                "ALTER TABLE [dbo].[Account] DROP COLUMN [JobOccupation]; " +
                "END;");

            // Support databases that already received Department from an earlier
            // manual/schema script as well as databases that still have JobOccupation.
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NOT NULL " +
                "AND COL_LENGTH(N'dbo.Account', N'Department') IS NULL " +
                "BEGIN EXEC sp_rename N'[dbo].[Account].[JobOccupation]', N'Department', N'COLUMN'; END;");

            migrationBuilder.Sql(
                "UPDATE [Account] SET [Department] = N'' WHERE [Department] IS NULL;");

            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Account', N'Department') IS NOT NULL " +
                "BEGIN ALTER TABLE [dbo].[Account] ALTER COLUMN [Department] nvarchar(100) NOT NULL; END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Account', N'Department') IS NOT NULL " +
                "AND COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NULL " +
                "BEGIN EXEC sp_rename N'[dbo].[Account].[Department]', N'JobOccupation', N'COLUMN'; END;");

            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NOT NULL " +
                "BEGIN ALTER TABLE [dbo].[Account] ALTER COLUMN [JobOccupation] nvarchar(100) NULL; END;");
        }
    }
}
