using MediatR;

namespace OmniHogar.Application.Features.Auth;

/// <summary>Request DTO for creating a new customer account.</summary>
public record RegisterCommand(string Email, string Password, string FirstName, string LastName, string? Phone)
    : IRequest<Guid>;
