using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Exceptions;

namespace SchoolAdmission.Api.Middleware;

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
        _logger.LogWarning(exception, "Request failed with error: {Message}", exception.Message);

        var statusCode = HttpStatusCode.BadRequest;
        var error = exception.Message;

        switch (exception)
        {
            case ResourceNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                break;
            case ConflictException:
                statusCode = HttpStatusCode.Conflict;
                break;
            case ForbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                break;
            case BadInputException:
            case BadHttpRequestException:
            case JsonException:
                statusCode = HttpStatusCode.BadRequest;
                break;
            default:
                _logger.LogError(exception, "Unhandled error occurred.");
                statusCode = HttpStatusCode.BadRequest;
                error = "An error occurred while processing the request.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error,
            statusCode = (int)statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
