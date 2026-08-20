using Microsoft.OpenApi;

namespace OmniHogar.WebApi.Extensions;

public static class SwaggerServiceExtensions
{
    private const string BearerSchemeId = "Bearer";

    public static IServiceCollection AddOmniHogarSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OmniHogar API",
                Version = "v1",
                Description = "Backend API for the OmniHogar Angular frontend.",
            });

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token.",
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(BearerSchemeId, null),
                    []
                },
            });
        });

        return services;
    }
}
