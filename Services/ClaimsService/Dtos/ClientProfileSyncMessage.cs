namespace ClaimsService.Dtos;

public sealed record ClientProfileSyncMessage(
    string PolicyNumber,
    string? FullName,
    string? Email);

public static class ClientProfileEventTypes
{
    public const string Upsert = "ClientProfileUpsert";
    public const string Delete = "ClientProfileDelete";
}
