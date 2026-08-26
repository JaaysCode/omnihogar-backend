namespace OmniHogar.WebApi.Contracts;

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
