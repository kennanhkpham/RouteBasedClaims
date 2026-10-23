using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialAzureMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    clientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    clientPolicyNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    clientFullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    clientEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.clientId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Client");
        }
    }
}
