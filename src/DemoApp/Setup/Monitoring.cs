using Microsoft.AspNetCore.HttpLogging;

namespace DemoApp.Setup;

/// <summary>Logs every incoming request (method, path, status, duration) as one line.</summary>
public static class Monitoring
{
    public static IServiceCollection AddMonitoring(this IServiceCollection services) =>
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                                    | HttpLoggingFields.RequestPath
                                    | HttpLoggingFields.ResponseStatusCode
                                    | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

    public static WebApplication UseMonitoring(this WebApplication app)
    {
        app.UseHttpLogging();
        return app;
    }
}
