namespace ClientsService.Dtos;

public sealed class ClientDto
{
    public int ClientId { get; init; }
    public string ClientPolicyNum { get; init; } = string.Empty;
    public string ClientFullName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;
}

public sealed class UpsertClientDto
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
