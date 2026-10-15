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
    public async Task<ActionResult<ClaimResponseDto>> SubmitClaim(
        [FromBody] CreateClaimDto dto,
        CancellationToken cancellationToken)
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

        await UpsertClientAsync(claim, cancellationToken);
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetClaimByNumber),
            new { claimNumber = claim.ClaimNumber },
            MapToDto(claim));
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

    // GET: api/claims (Admin claim review)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClaimResponseDto>>> GetClaims(
        CancellationToken cancellationToken)
    {
        var claims = await _context.Claims
            .Include(c => c.PrescriptionDetail)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return Ok(claims);
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

    // PUT: api/claims/{claimNumber} (Create or replace a claim)
    [HttpPut("{claimNumber}")]
    public async Task<ActionResult<ClaimResponseDto>> PutClaim(
        string claimNumber,
        [FromBody] CreateClaimDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(claimNumber))
        {
            return BadRequest(new { message = "Claim number is required." });
        }

        if (dto.PrescriptionDetail is null)
        {
            return BadRequest(new { message = "Prescription details are required." });
        }

        var normalizedClaimNumber = claimNumber.Trim();
        var claim = await _context.Claims
            .Include(c => c.PrescriptionDetail)
            .FirstOrDefaultAsync(
                c => c.ClaimNumber == normalizedClaimNumber,
                cancellationToken);
        var isNewClaim = claim is null;

        if (isNewClaim)
        {
            claim = new Claim
            {
                ClaimNumber = normalizedClaimNumber,
                Status = "Submitted",
                CreatedAt = DateTime.UtcNow,
                PrescriptionDetail = new PrescriptionDetails()
            };
            _context.Claims.Add(claim);
        }

        claim!.PolicyNumber = dto.PolicyNumber;
        claim.FullName = dto.FullName;
        claim.Email = dto.Email;
        claim.AmountCost = dto.AmountCost;
        claim.PrescriptionDetail ??= new PrescriptionDetails();
        claim.PrescriptionDetail.DoctorName = dto.PrescriptionDetail.DoctorName;
        claim.PrescriptionDetail.PrescriptionNumber = dto.PrescriptionDetail.PrescriptionNumber;
        claim.PrescriptionDetail.MedicationName = dto.PrescriptionDetail.MedicationName;
        claim.PrescriptionDetail.DatePrescribed = dto.PrescriptionDetail.DatePrescribed;
        await UpsertClientAsync(claim, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (isNewClaim)
        {
            return CreatedAtAction(
                nameof(GetClaimByNumber),
                new { claimNumber = claim.ClaimNumber },
                MapToDto(claim));
        }

        return Ok(MapToDto(claim));
    }
    // PATCH: api/claims/{id}/status (Approve/Reject Claim)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateStatusDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "A claim status is required." });
        }

        var updatedCount = await _context.Claims
            .Where(claim => claim.ClaimId == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(claim => claim.Status, dto.Status),
                cancellationToken);

        return updatedCount == 0 ? NotFound() : NoContent();
    }

    private async Task UpsertClientAsync(Claim claim, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(
            existing => existing.ClientPolicyNum == claim.PolicyNumber,
            cancellationToken);

        if (client is null)
        {
            _context.Clients.Add(new Client
            {
                ClientPolicyNum = claim.PolicyNumber,
                ClientFullName = claim.FullName,
                ClientEmail = claim.Email
            });
            return;
        }

        client.ClientFullName = claim.FullName;
        client.ClientEmail = claim.Email;
    }

    private static ClaimResponseDto MapToDto(Claim claim) => new()
    {
        ClaimId = claim.ClaimId,
        ClaimNumber = claim.ClaimNumber,
        PolicyNumber = claim.PolicyNumber,
        FullName = claim.FullName,
        Email = claim.Email,
        AmountCost = claim.AmountCost,
        Status = claim.Status,
        CreatedAt = claim.CreatedAt,
        PrescriptionDetail = new PrescriptionDto
        {
            PrescriptionId = claim.PrescriptionDetail?.PrescriptionId ?? 0,
            DoctorName = claim.PrescriptionDetail?.DoctorName ?? string.Empty,
            PrescriptionNumber = claim.PrescriptionDetail?.PrescriptionNumber ?? string.Empty,
            MedicationName = claim.PrescriptionDetail?.MedicationName ?? string.Empty,
            DatePrescribed = claim.PrescriptionDetail?.DatePrescribed ?? DateTime.MinValue,
            PrescriptionClaimId = claim.PrescriptionDetail?.PrescriptionClaimId ?? claim.ClaimId
        }
    };

    // GET: api/claims/{claimNumber} (Delete Claim)
    [HttpDelete("{claimNumber}")]
    public async Task<ActionResult<ClaimResponseDto>> DeleteClaim(
        string claimNumber,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claim = await _context.Claims
            .Include(c => c.PrescriptionDetail)
            .FirstOrDefaultAsync(
                c => c.ClaimNumber == claimNumber,
                cancellationToken);

        if (claim == null) return NotFound(new { message = "Claim not found." });

        var deletedClaim = MapToDto(claim);
        var policyNumber = claim.PolicyNumber;
        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync(cancellationToken);

        var client = await _context.Clients.FirstOrDefaultAsync(
            existing => existing.ClientPolicyNum == policyNumber,
            cancellationToken);
        if (client is not null)
        {
            var latestClaim = await _context.Claims
                .Where(existing => existing.PolicyNumber == policyNumber)
                .OrderByDescending(existing => existing.CreatedAt)
                .ThenByDescending(existing => existing.ClaimId)
                .Select(existing => new
                {
                    existing.FullName,
                    existing.Email
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (latestClaim is null)
            {
                _context.Clients.Remove(client);
            }
            else
            {
                client.ClientFullName = latestClaim.FullName;
                client.ClientEmail = latestClaim.Email;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(deletedClaim);
    }
}