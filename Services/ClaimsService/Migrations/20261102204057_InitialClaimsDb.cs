using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialClaimsDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claim",
                columns: table => new
                {
                    claimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    claimNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    policyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claim", x => x.claimId);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionDetails",
                columns: table => new
                {
                    prescriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    prescriptionNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    doctorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    datePrescribed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    medicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prescriptionClaimId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionDetails", x => x.prescriptionId);
                    table.ForeignKey(
                        name: "FK_PrescriptionDetails_Claim_prescriptionClaimId",
                        column: x => x.prescriptionClaimId,
                        principalTable: "Claim",
                        principalColumn: "claimId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDetails_prescriptionClaimId",
                table: "PrescriptionDetails",
                column: "prescriptionClaimId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionDetails");

            migrationBuilder.DropTable(
                name: "Claim");
        }
    }
}
