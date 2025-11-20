using System.Net;
using System.Text.Json;
using OrderManagement.Exceptions;

namespace OrderManagement.Common.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionMiddleware> logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ErrorResponse error;
        int statusCode;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = validationException.StatusCode;
                error = new ErrorResponse(validationException.ErrorCode, validationException.Message, validationException.Errors);
                break;
            case BaseException baseException:
                statusCode = baseException.StatusCode;
                error = new ErrorResponse(baseException.ErrorCode, baseException.Message);
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                error = new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred.");
                logger.LogError(exception, "Unhandled exception");
                break;
        }

        error.TraceId = context.TraceIdentifier;
        context.Response.StatusCode = statusCode;

        var payload = JsonSerializer.Serialize(error);
        await context.Response.WriteAsync(payload);
    }
}

internal record ErrorResponse(string ErrorCode, string Message, IReadOnlyCollection<string>? Details = null)
{
    public string TraceId { get; set; } = string.Empty;
}

