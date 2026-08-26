using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Domain.Entities;
using OmniHogar.Infrastructure.Identity;
using OmniHogar.WebApi.Contracts;

namespace OmniHogar.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ISender _sender;

    public AuthController(
        IApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ISender sender)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _sender = sender;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var userId = await _sender.Send(command, cancellationToken);
        var user = await _context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        return await BuildAuthResponse(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.Status)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        return await BuildAuthResponse(user);
    }

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var roles = await _context.Users
            .Where(u => u.Id == user.Id)
            .SelectMany(u => u.UserRoles.Select(ur => ur.Role.Name))
            .ToListAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthResponse(accessToken, refreshToken, expiresAtUtc);
    }
}
