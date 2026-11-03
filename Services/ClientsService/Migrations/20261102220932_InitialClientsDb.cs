using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialClientsDb : Migration
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
                    clientEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicyNumberKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.clientId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_PolicyNumberKey",
                table: "Client",
                column: "PolicyNumberKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Client");
        }
    }
}
