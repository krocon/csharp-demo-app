namespace DemoApp.Setup;

/// <summary>
/// Enables the two streaming techniques:
/// WebSockets (two-way) need a middleware; Server-Sent Events (one-way) are built into
/// .NET 10 as a result type (<c>TypedResults.ServerSentEvents</c>) and need no setup.
/// </summary>
public static class Sockets
{
    public static WebApplication UseSockets(this WebApplication app)
    {
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30),
        });
        return app;
    }
}
