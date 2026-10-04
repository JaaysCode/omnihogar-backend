using MediatR;

namespace OmniHogar.Application.Features.Auth;

/// <summary>Consumes a password-recovery token to set a new password (HU-15 crit. 3).</summary>
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
