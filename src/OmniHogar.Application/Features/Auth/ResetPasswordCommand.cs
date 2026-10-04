using MediatR;

namespace OmniHogar.Application.Features.Auth;

/// <summary>
/// Consumes a password-recovery token to set a new password (HU-15 crit. 3). Explicitly
/// <see cref="IRequest{Unit}"/> — see <see cref="ForgotPasswordCommand"/> for why the bare
/// <c>IRequest</c> marker must be avoided (it skips the validation pipeline).
/// </summary>
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;
