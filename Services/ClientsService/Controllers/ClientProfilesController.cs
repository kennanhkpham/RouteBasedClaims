using ClientsService.Data;
using ClientsService.Dtos;
using ClientsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientsService.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Admin")]
public sealed class ClientProfilesController(ClientsDbContext context) : ControllerBase
{
    [HttpGet("{policyNumber}")]
    public async Task<ActionResult<ClientDto>> GetByPolicyNumber(
        string policyNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(policyNumber))
        {
            return BadRequest(new { message = "Policy number is required." });
        }

        var policyNumberKey = PolicyNumberKey.Create(policyNumber);
        var client = await context.Clients.SingleOrDefaultAsync(
            item => item.PolicyNumberKey == policyNumberKey,
            cancellationToken);

        if (client is null)
        {
            return NotFound(new { message = "Client not found." });
        }

        return Ok(new ClientDto
        {
            ClientId = client.ClientId,
            ClientPolicyNum = client.ClientPolicyNum,
            ClientFullName = client.ClientFullName,
            ClientEmail = client.ClientEmail
        });
    }
}
