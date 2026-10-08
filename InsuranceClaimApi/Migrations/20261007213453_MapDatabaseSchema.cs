using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimApi.Migrations
{
    /// <inheritdoc />
    public partial class MapDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionDetails_Claims_ClaimId",
                table: "PrescriptionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Claims",
                table: "Claims");

            migrationBuilder.RenameTable(
                name: "Claims",
                newName: "Claim");

            migrationBuilder.RenameColumn(
                name: "PrescriptionNumber",
                table: "PrescriptionDetails",
                newName: "prescriptionNumber");

            migrationBuilder.RenameColumn(
                name: "MedicationName",
                table: "PrescriptionDetails",
                newName: "medicationName");

            migrationBuilder.RenameColumn(
                name: "DoctorName",
                table: "PrescriptionDetails",
                newName: "doctorName");

            migrationBuilder.RenameColumn(
                name: "DatePrescribed",
                table: "PrescriptionDetails",
                newName: "datePrescribed");

            migrationBuilder.RenameColumn(
                name: "ClaimId",
                table: "PrescriptionDetails",
                newName: "prescriptionClaimId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PrescriptionDetails",
                newName: "prescriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_PrescriptionDetails_ClaimId",
                table: "PrescriptionDetails",
                newName: "IX_PrescriptionDetails_prescriptionClaimId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Claim",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "PolicyNumber",
                table: "Claim",
                newName: "policyNumber");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Claim",
                newName: "fullName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Claim",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Claim",
                newName: "createdAt");

            migrationBuilder.RenameColumn(
                name: "ClaimNumber",
                table: "Claim",
                newName: "claimNumber");

            migrationBuilder.RenameColumn(
                name: "ClaimId",
                table: "Claim",
                newName: "claimId");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Claim",
                newName: "amountCost");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Claim",
                table: "Claim",
                column: "claimId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionDetails_Claim_prescriptionClaimId",
                table: "PrescriptionDetails",
                column: "prescriptionClaimId",
                principalTable: "Claim",
                principalColumn: "claimId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionDetails_Claim_prescriptionClaimId",
                table: "PrescriptionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Claim",
                table: "Claim");

            migrationBuilder.RenameTable(
                name: "Claim",
                newName: "Claims");

            migrationBuilder.RenameColumn(
                name: "prescriptionNumber",
                table: "PrescriptionDetails",
                newName: "PrescriptionNumber");

            migrationBuilder.RenameColumn(
                name: "medicationName",
                table: "PrescriptionDetails",
                newName: "MedicationName");

            migrationBuilder.RenameColumn(
                name: "doctorName",
                table: "PrescriptionDetails",
                newName: "DoctorName");

            migrationBuilder.RenameColumn(
                name: "datePrescribed",
                table: "PrescriptionDetails",
                newName: "DatePrescribed");

            migrationBuilder.RenameColumn(
                name: "prescriptionClaimId",
                table: "PrescriptionDetails",
                newName: "ClaimId");

            migrationBuilder.RenameColumn(
                name: "prescriptionId",
                table: "PrescriptionDetails",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_PrescriptionDetails_prescriptionClaimId",
                table: "PrescriptionDetails",
                newName: "IX_PrescriptionDetails_ClaimId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Claims",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "policyNumber",
                table: "Claims",
                newName: "PolicyNumber");

            migrationBuilder.RenameColumn(
                name: "fullName",
                table: "Claims",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Claims",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "createdAt",
                table: "Claims",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "claimNumber",
                table: "Claims",
                newName: "ClaimNumber");

            migrationBuilder.RenameColumn(
                name: "claimId",
                table: "Claims",
                newName: "ClaimId");

            migrationBuilder.RenameColumn(
                name: "amountCost",
                table: "Claims",
                newName: "Amount");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Claims",
                table: "Claims",
                column: "ClaimId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionDetails_Claims_ClaimId",
                table: "PrescriptionDetails",
                column: "ClaimId",
                principalTable: "Claims",
                principalColumn: "ClaimId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
