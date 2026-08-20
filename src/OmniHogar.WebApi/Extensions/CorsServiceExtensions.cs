namespace OmniHogar.WebApi.Extensions;

public static class CorsServiceExtensions
{
    public const string AngularClientPolicy = "AngularClient";

    /// <summary>
    /// CORS policy scoped to the omnihogar-frontend Angular app (dev server + configurable prod origins).
    /// </summary>
    public static IServiceCollection AddOmniHogarCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy(AngularClientPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
