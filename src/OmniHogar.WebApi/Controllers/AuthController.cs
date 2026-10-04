using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
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

        return await BuildAuthResponse(user, cancellationToken);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ValidateLoginFields(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.Status)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        return await BuildAuthResponse(user, cancellationToken);
    }

    /// <summary>
    /// Exchanges a still-valid refresh token for a new access/refresh pair (rotation): the
    /// presented token is revoked and replaced, so it can never be redeemed a second time. If a
    /// token that's already been rotated (or revoked) shows up again, every other active token
    /// for that user is revoked too — that pattern only happens if a refresh token leaked and
    /// both the legitimate client and an attacker tried to use it.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var presented = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (presented is null || presented.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Sesión expirada. Inicia sesión de nuevo." });
        }

        if (presented.RevokedAt is not null)
        {
            await RevokeAllActiveTokensAsync(presented.UserId, cancellationToken);
            return Unauthorized(new { message = "Sesión inválida. Inicia sesión de nuevo." });
        }

        if (!presented.User.Status)
        {
            return Unauthorized(new { message = "Cuenta inactiva." });
        }

        var (rawRefreshToken, newEntity) = CreateRefreshToken(presented.UserId);
        presented.RevokedAt = DateTime.UtcNow;
        presented.ReplacedByTokenId = newEntity.Id;
        _context.RefreshTokens.Add(newEntity);

        var accessToken = await BuildAccessTokenAsync(presented.User, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        return new AuthResponse(accessToken, rawRefreshToken, expiresAtUtc);
    }

    /// <summary>Starts password recovery for a registered email (HU-15 crit. 1/2).</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Ok(new { message = "Revisa tu correo para continuar con la recuperación." });
    }

    /// <summary>Consumes a password-recovery token to set a new password (HU-15 crit. 3).</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Ok(new { message = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // HU-03 crit. 3: campos obligatorios en el inicio de sesión — mismo shape 400
    // {status,title,errors} que el resto de validaciones, con la redacción de RegisterCommandValidator.
    private static void ValidateLoginFields(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["Email"] = ["El correo electrónico es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["Password"] = ["La contraseña es obligatoria."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task<string> BuildAccessTokenAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .Distinct()
            .ToListAsync(cancellationToken);

        return _tokenService.GenerateAccessToken(user, roles, permissions);
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId)
    {
        var rawToken = _tokenService.GenerateRefreshToken();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = _tokenService.HashRefreshToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
        };
        return (rawToken, entity);
    }

    private async Task<AuthResponse> BuildAuthResponse(User user, CancellationToken cancellationToken)
    {
        var accessToken = await BuildAccessTokenAsync(user, cancellationToken);
        var (rawRefreshToken, entity) = CreateRefreshToken(user.Id);

        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        return new AuthResponse(accessToken, rawRefreshToken, expiresAtUtc);
    }
}
