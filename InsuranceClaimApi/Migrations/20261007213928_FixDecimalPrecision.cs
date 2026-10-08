using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimApi.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "amountCost",
                table: "Claim",
                newName: "AmountCost");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AmountCost",
                table: "Claim",
                newName: "amountCost");
        }
    }
}
