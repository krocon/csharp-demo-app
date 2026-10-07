using Microsoft.AspNetCore.Diagnostics;

namespace DemoApp.Setup;

/// <summary>JSON body of every error response, e.g. {"message":"Task 42 not found"}.</summary>
public record ErrorResponse(string Message);

/// <summary>Thrown when a requested resource does not exist; becomes a 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Thrown when a request body is invalid; becomes a 400.</summary>
public class ValidationException(string message) : Exception(message);

/// <summary>Maps exceptions to clean JSON error responses with the right HTTP status.</summary>
public class ErrorHandler(ILogger<ErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest, e.Message),
            // Thrown by Minimal APIs for unparsable route/query values or malformed JSON.
            BadHttpRequestException e => (e.StatusCode, e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled error");
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(message), cancellationToken);
        return true;
    }
}

public static class ErrorHandling
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<ErrorHandler>();
        services.AddProblemDetails();

        // Let Minimal APIs throw BadHttpRequestException (instead of silently
        // answering 400) so ErrorHandler can turn it into an ErrorResponse.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        return services;
    }

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
