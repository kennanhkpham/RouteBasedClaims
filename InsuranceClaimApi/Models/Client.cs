using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimApi.Models;

[Table("Client")]
public class Client
{
    [Key]
    [Column("clientId")]
    public int ClientId { get; set; }

    [Required]
    [Column("clientPolicyNum")]
    public string ClientPolicyNum { get; set; } = string.Empty;

    [Required]
    [Column("clientFullName")]
    public string ClientFullName { get; set; } = string.Empty;

    [Required]
    [Column("clientEmail")]
    public string ClientEmail { get; set; } = string.Empty;
}