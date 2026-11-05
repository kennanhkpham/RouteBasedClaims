namespace AuthService.Dtos;

public sealed record CreatedAccountResponse(int Id, string Username, bool IsAdmin, string Department);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Role);
