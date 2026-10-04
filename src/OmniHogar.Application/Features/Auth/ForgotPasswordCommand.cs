using MediatR;

namespace OmniHogar.Application.Features.Auth;

/// <summary>Request to start password recovery for a registered email (HU-15 crit. 1/2).</summary>
public record ForgotPasswordCommand(string Email) : IRequest;
