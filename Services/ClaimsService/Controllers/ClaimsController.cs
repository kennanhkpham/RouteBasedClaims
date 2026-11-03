using System.Text.Json;
using ClaimsService.Data;
using ClaimsService.Dtos;
using ClaimsService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaimsController(ClaimsDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClaimResponseDto>> SubmitClaim(
        [FromBody] CreateClaimDto dto,
        CancellationToken cancellationToken)
    {
        var claim = new Claim
        {
            ClaimNumber = $"CLM-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
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

        context.Claims.Add(claim);
        QueueClientUpsert(claim);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetClaimByNumber),
            new { claimNumber = claim.ClaimNumber },
            MapToDto(claim));
    }

    [HttpGet("{claimNumber}")]
    public async Task<ActionResult<ClaimResponseDto>> GetClaimByNumber(
        string claimNumber,
        CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .Include(item => item.PrescriptionDetail)
            .FirstOrDefaultAsync(item => item.ClaimNumber == claimNumber, cancellationToken);

        if (claim is null)
        {
            return NotFound(new { message = "Claim not found." });
        }

        return Ok(MapToDto(claim));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ClaimResponseDto>>> GetClaims(
        CancellationToken cancellationToken)
    {
        var claims = await context.Claims
            .Include(item => item.PrescriptionDetail)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => MapToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(claims);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ClaimResponseDto>>> GetPendingClaims(
        CancellationToken cancellationToken)
    {
        var claims = await context.Claims
            .Include(item => item.PrescriptionDetail)
            .Where(item => item.Status == "Submitted" || item.Status == "Pending Authorization")
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => MapToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(claims);
    }

    [HttpPut("{claimNumber}")]
    [Authorize(Roles = "Admin")]
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
        var claim = await context.Claims
            .Include(item => item.PrescriptionDetail)
            .FirstOrDefaultAsync(
                item => item.ClaimNumber == normalizedClaimNumber,
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
            context.Claims.Add(claim);
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
        QueueClientUpsert(claim);
        await context.SaveChangesAsync(cancellationToken);

        if (isNewClaim)
        {
            return CreatedAtAction(
                nameof(GetClaimByNumber),
                new { claimNumber = claim.ClaimNumber },
                MapToDto(claim));
        }

        return Ok(MapToDto(claim));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateStatusDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "A claim status is required." });
        }

        var updatedCount = await context.Claims
            .Where(claim => claim.ClaimId == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(claim => claim.Status, dto.Status),
                cancellationToken);

        return updatedCount == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{claimNumber}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClaimResponseDto>> DeleteClaim(
        string claimNumber,
        CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .Include(item => item.PrescriptionDetail)
            .FirstOrDefaultAsync(
                item => item.ClaimNumber == claimNumber,
                cancellationToken);

        if (claim is null)
        {
            return NotFound(new { message = "Claim not found." });
        }

        var deletedClaim = MapToDto(claim);
        var latestClaim = await context.Claims
            .Where(item => item.PolicyNumber == claim.PolicyNumber && item.ClaimId != claim.ClaimId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.ClaimId)
            .Select(item => new
            {
                item.PolicyNumber,
                item.FullName,
                item.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        context.Claims.Remove(claim);
        if (latestClaim is null)
        {
            QueueClientDelete(claim.PolicyNumber);
        }
        else
        {
            QueueClientUpsert(latestClaim.PolicyNumber, latestClaim.FullName, latestClaim.Email);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Ok(deletedClaim);
    }

    private void QueueClientUpsert(Claim claim) =>
        QueueClientUpsert(claim.PolicyNumber, claim.FullName, claim.Email);

    private void QueueClientUpsert(string policyNumber, string fullName, string email)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            EventType = ClientProfileEventTypes.Upsert,
            Payload = JsonSerializer.Serialize(
                new ClientProfileSyncMessage(policyNumber, fullName, email)),
            OccurredAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        });
    }

    private void QueueClientDelete(string policyNumber)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            EventType = ClientProfileEventTypes.Delete,
            Payload = JsonSerializer.Serialize(
                new ClientProfileSyncMessage(policyNumber, null, null)),
            OccurredAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        });
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
}
