using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimsService.Models;

[Table("PrescriptionDetails")]
public class PrescriptionDetails
{
    [Key]
    [Column("prescriptionId")]
    public int PrescriptionId { get; set; }

    [Required]
    [Column("prescriptionNumber")]
    public string PrescriptionNumber { get; set; } = string.Empty;

    [Required]
    [Column("doctorName")]
    public string DoctorName { get; set; } = string.Empty;

    [Required]
    [Column("datePrescribed")]
    public DateTime DatePrescribed { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("medicationName")]
    public string MedicationName { get; set; } = string.Empty;

    [Column("prescriptionClaimId")]
    public int PrescriptionClaimId { get; set; }

    [ForeignKey(nameof(PrescriptionClaimId))]
    public Claim? Claim { get; set; }
}
