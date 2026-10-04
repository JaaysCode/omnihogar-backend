using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Constants;
using OmniHogar.Infrastructure.Email;
using OmniHogar.Infrastructure.Identity;
using OmniHogar.Infrastructure.Llm;
using OmniHogar.Infrastructure.Payments;
using OmniHogar.Infrastructure.Persistence;
using OmniHogar.Infrastructure.Services;

namespace OmniHogar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Auth: users/roles/permissions come from our own tables (see Domain.Entities.User/Role/Permission),
        // not ASP.NET Core Identity. Password hashing goes through PasswordHasher<User> directly.
        services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<Domain.Entities.User>,
            Microsoft.AspNetCore.Identity.PasswordHasher<Domain.Entities.User>>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<ITokenService, TokenService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Stripe Checkout (HU-08/HU-09). SecretKey is optional at startup — an unconfigured
        // gateway just fails checkout calls with a "communication error", it doesn't stop the
        // whole API from booting like Jwt:Secret does.
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddScoped<IPaymentGatewayUrls>(sp => sp.GetRequiredService<IOptions<StripeOptions>>().Value);
        services.AddScoped<IPaymentGatewayClient, StripeCheckoutClient>();

        // Password recovery (HU-15) — sends real email via an SMTP relay (Brevo).
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Chatbot NLU (HU-19) — OpenRouter, modelo gratuito. Requerido: todo el chatbot deja de
        // funcionar sin esto, así que falla fuerte en el arranque (vía docker-compose's ${...:?})
        // en vez de fallar silenciosamente en cada mensaje, a diferencia de Stripe.
        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));
        services.AddHttpClient<OpenRouterClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            // OpenRouter pide identificar la app integradora — opcional pero buena práctica.
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://omnihogar.local");
            client.DefaultRequestHeaders.Add("X-Title", "OmniHogar");
        });
        services.AddScoped<ILlmClient>(sp => sp.GetRequiredService<OpenRouterClient>());

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        // Una política por permiso: [Authorize(Policy = AppPermissions.X)] exige el claim
        // "permission" == X, que el TokenService emite a partir de role_permissions.
        services.AddAuthorization(options =>
        {
            foreach (var permission in AppPermissions.All)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
            }
        });

        return services;
    }
}
