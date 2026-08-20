using OmniHogar.Application;
using OmniHogar.Infrastructure;
using OmniHogar.WebApi.Extensions;
using OmniHogar.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. Clean Architecture layering: Application -> Infrastructure -> WebApi.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOmniHogarSwagger();
builder.Services.AddOmniHogarCors(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(CorsServiceExtensions.AngularClientPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
