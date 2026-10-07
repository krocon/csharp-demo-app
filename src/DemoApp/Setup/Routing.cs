using DemoApp.Live;
using DemoApp.Tasks;

namespace DemoApp.Setup;

/// <summary>Registers all HTTP routes of the application.</summary>
public static class Routing
{
    public static WebApplication MapRoutes(this WebApplication app)
    {
        app.MapGet("/", () => Results.Content(
            """
            <h1>Hello, ASP.NET Core!</h1>
            <p><a href="/live/">Open the live demo &rarr;</a></p>
            """,
            "text/html"));

        app.MapGet("/health", () => new { status = "UP" });

        app.MapTaskRoutes();
        app.MapLiveRoutes();

        // Browser demo page for the live endpoints: http://localhost:8080/live/
        // (served from wwwroot/live/index.html)
        app.UseDefaultFiles();
        app.UseStaticFiles();

        return app;
    }
}
