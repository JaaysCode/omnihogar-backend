namespace OmniHogar.WebApi.Contracts;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? Phone);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
