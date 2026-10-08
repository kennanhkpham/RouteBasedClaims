namespace InsuranceClaimApi.Dtos;

public class PrescriptionDto
{
    public int PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime DatePrescribed { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int PrescriptionClaimId { get; set; }
}

public class ClaimResponseDto
{
    public int ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal AmountCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public PrescriptionDto PrescriptionDetail { get; set; } = null!;
}

public class ClientDto
{
    public int ClientId { get; set; }
    public string ClientPolicyNumber { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
}

public class CreateClaimDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal AmountCost { get; set; }
    public PrescriptionDto PrescriptionDetail { get; set; } = null!;
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}