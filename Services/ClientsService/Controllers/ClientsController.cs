using ClientsService.Data;
using ClientsService.Dtos;
using ClientsService.Models;
using ClientsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientsService.Controllers;

[ApiController]
[Route("internal/clients")]
[Authorize(AuthenticationSchemes = ServiceKeyAuthenticationHandler.SchemeName)]
public sealed class ClientsController(ClientsDbContext context) : ControllerBase
{
    [HttpPut("{policyNumber}")]
    public async Task<ActionResult<ClientDto>> Upsert(
        string policyNumber,
        [FromBody] UpsertClientDto dto,
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
            client = new Client
            {
                ClientPolicyNum = policyNumber,
                PolicyNumberKey = policyNumberKey,
                ClientFullName = dto.FullName,
                ClientEmail = dto.Email
            };
            context.Clients.Add(client);
        }
        else
        {
            client.ClientPolicyNum = policyNumber;
            client.ClientFullName = dto.FullName;
            client.ClientEmail = dto.Email;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(client));
    }

    [HttpDelete("{policyNumber}")]
    public async Task<IActionResult> Delete(
        string policyNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(policyNumber))
        {
            return BadRequest(new { message = "Policy number is required." });
        }

        var policyNumberKey = PolicyNumberKey.Create(policyNumber);
        await context.Clients
            .Where(item => item.PolicyNumberKey == policyNumberKey)
            .ExecuteDeleteAsync(cancellationToken);

        return NoContent();
    }

    private static ClientDto MapToDto(Client client) => new()
    {
        ClientId = client.ClientId,
        ClientPolicyNum = client.ClientPolicyNum,
        ClientFullName = client.ClientFullName,
        ClientEmail = client.ClientEmail
    };
}
