using System.Net;
using System.Text.Json;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.WebApi.Middleware;

/// <summary>
/// Translates Application/Domain exceptions into consistent JSON problem responses
/// so the Angular client always gets a predictable error shape.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, errors) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null as IDictionary<string, string[]>),
            ValidationException validationEx => (HttpStatusCode.BadRequest, "Hay errores de validación.", validationEx.Errors),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, exception.Message, null),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", null),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            status = (int)statusCode,
            title,
            errors,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
