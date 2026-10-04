using MediatR;

namespace OmniHogar.Application.Features.Auth;

/// <summary>
/// Request to start password recovery for a registered email (HU-15 crit. 1/2).
/// Explicitly <see cref="IRequest{Unit}"/> (not the bare <c>IRequest</c> marker) — the bare
/// marker makes <c>ISender.Send</c> resolve to MediatR's non-generic void overload, which does
/// not run the registered <c>IPipelineBehavior&lt;,&gt;</c> chain (validation never fired,
/// confirmed at runtime: an unregistered email reached the handler instead of being rejected).
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
