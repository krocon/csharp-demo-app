using System.Net.ServerSentEvents;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using DemoApp.Tasks;

namespace DemoApp.Live;

/// <summary>
/// Live endpoints:
///
///   WS  /ws/tasks      two-way: receive all task events, send a text message to create a task
///   SSE /tasks/events  one-way: receive all task events as Server-Sent Events
/// </summary>
public static class LiveRoutes
{
    public static IEndpointRouteBuilder MapLiveRoutes(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/tasks", HandleWebSocketAsync);

        app.MapGet("/tasks/events", (TaskEventBus eventBus, CancellationToken cancellationToken) =>
            TypedResults.ServerSentEvents(StreamEvents(eventBus, cancellationToken)));

        return app;
    }

    private static async Task HandleWebSocketAsync(
        HttpContext context, ITaskRepository repository, TaskEventBus eventBus)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

        // Server -> client: forward every task event as a JSON text message.
        // Subscribe *before* reading, so no event published from now on is missed.
        var forwarding = ForwardEventsAsync(socket, eventBus.Subscribe(stop.Token), stop.Token);

        // Client -> server: every text message becomes a new task.
        try
        {
            while (await ReceiveTextAsync(socket, stop.Token) is { } text)
            {
                var title = text.Trim();
                if (title.Length == 0) continue;

                var task = await repository.CreateAsync(title);
                eventBus.Publish(new TaskEvent.Created(task));
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException)
        {
            // Client went away without a proper close handshake – nothing to do.
        }
        finally
        {
            await stop.CancelAsync();
            await forwarding;
        }

        if (socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    private static async Task ForwardEventsAsync(
        WebSocket socket, ChannelReader<TaskEvent> events, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var taskEvent in events.ReadAllAsync(cancellationToken))
            {
                var bytes = Encoding.UTF8.GetBytes(taskEvent.ToJson());
                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException)
        {
            // Connection closed while sending – the receive loop ends the session.
        }
    }

    /// <summary>Reads one complete text message, or returns null when the client closes.</summary>
    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4 * 1024];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return null;

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage) continue;

            if (result.MessageType == WebSocketMessageType.Text)
            {
                return Encoding.UTF8.GetString(message.ToArray());
            }
            message.SetLength(0); // ignore binary messages
        }
    }

    private static async IAsyncEnumerable<SseItem<string>> StreamEvents(
        TaskEventBus eventBus, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var events = eventBus.Subscribe(cancellationToken);
        await foreach (var taskEvent in events.ReadAllAsync(cancellationToken))
        {
            yield return new SseItem<string>(taskEvent.ToJson(), taskEvent.Type);
        }
    }
}
