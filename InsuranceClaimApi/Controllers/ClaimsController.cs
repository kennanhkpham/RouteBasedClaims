using InsuranceClaimApi.Dtos;
using InsuranceClaimApi.Data;
using InsuranceClaimApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClaimsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/claims (Submit Claim Intake)
    [HttpPost]
    public async Task<ActionResult<ClaimResponseDto>> SubmitClaim([FromBody] CreateClaimDto dto)
    {
        var claim = new Claim
        {
            ClaimNumber = $"CLM-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            PolicyNumber = dto.PolicyNumber,
            FullName = dto.FullName,
            Email = dto.Email,
            AmountCost = dto.AmountCost,
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow,
            PrescriptionDetail = new PrescriptionDetails
            {
                DoctorName = dto.PrescriptionDetail.DoctorName,
                PrescriptionNumber = dto.PrescriptionDetail.PrescriptionNumber,
                MedicationName = dto.PrescriptionDetail.MedicationName,
                DatePrescribed = dto.PrescriptionDetail.DatePrescribed
            }
        };

        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        var response = MapToDto(claim);
        return CreatedAtAction(nameof(GetClaimByNumber), new { claimNumber = claim.ClaimNumber }, response);
    }

    // GET: api/claims/{claimNumber} (Check Claim Status)
    [HttpGet("{claimNumber}")]
    public async Task<ActionResult<ClaimResponseDto>> GetClaimByNumber(string claimNumber)
    {
        var claim = await _context.Claims
            .Include(c => c.PrescriptionDetail)
            .FirstOrDefaultAsync(c => c.ClaimNumber == claimNumber);

        if (claim == null) return NotFound(new { message = "Claim not found." });

        return Ok(MapToDto(claim));
    }

    // GET: api/claims/pending (Reviewer View)
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<ClaimResponseDto>>> GetPendingClaims()
    {
        var claims = await _context.Claims
            .Include(c => c.PrescriptionDetail)
            .Where(c => c.Status == "Submitted" || c.Status == "Pending Authorization")
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();

        return Ok(claims);
    }

    // PATCH: api/claims/{id}/status (Approve/Reject Claim)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var claim = await _context.Claims.FindAsync(id);
        if (claim == null) return NotFound();

        claim.Status = dto.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ClaimResponseDto MapToDto(Claim claim) => new()
    {
        ClaimId = claim.ClaimId,
        ClaimNumber = claim.ClaimNumber,
        PolicyNumber = claim.PolicyNumber,
        AmountCost = claim.AmountCost,
        Status = claim.Status,
        CreatedAt = claim.CreatedAt,
        PrescriptionDetail = new PrescriptionDto
        {
            DoctorName = claim.PrescriptionDetail?.DoctorName ?? string.Empty,
            PrescriptionNumber = claim.PrescriptionDetail?.PrescriptionNumber ?? string.Empty,
            MedicationName = claim.PrescriptionDetail?.MedicationName ?? string.Empty,
            DatePrescribed = claim.PrescriptionDetail?.DatePrescribed ?? DateTime.MinValue
        }
    };
}