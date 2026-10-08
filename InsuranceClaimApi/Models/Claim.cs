using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimApi.Models;

[Table("Claim")]
public class Claim
{
    [Key]
    [Column("claimId")]
    public int ClaimId { get; set; }

    [Required]
    [Column("claimNumber")]
    public string ClaimNumber { get; set; } = string.Empty;

    [Required]
    [Column("policyNumber")]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required]
    [Column("fullName")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountCost { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = "Submitted"; // Submitted, Approved, Rejected, Pending Authorization

    [Required]
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 1-to-1 Relationship with Prescription Detail
    public PrescriptionDetails PrescriptionDetail { get; set; }
}