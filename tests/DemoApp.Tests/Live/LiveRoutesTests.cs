using System.Net.WebSockets;
using System.Text;
using DemoApp.Tasks;

namespace DemoApp.Tests.Live;

public class LiveRoutesTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Opens an in-memory WebSocket connection to /ws/tasks.</summary>
    private static Task<WebSocket> ConnectAsync(TestApp app)
    {
        var uri = new UriBuilder(app.Server.BaseAddress) { Scheme = "ws", Path = "/ws/tasks" }.Uri;
        return app.Server.CreateWebSocketClient().ConnectAsync(uri, CancellationToken.None);
    }

    private static Task SendTextAsync(WebSocket socket, string text) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

    private static async Task<TaskEvent> ReceiveEventAsync(WebSocket socket)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        var buffer = new byte[4 * 1024];
        var result = await socket.ReceiveAsync(buffer, timeout.Token);
        return TaskEvent.FromJson(Encoding.UTF8.GetString(buffer, 0, result.Count));
    }

    [Fact]
    public async Task Text_message_creates_a_task_and_is_broadcast_back()
    {
        await using var app = new TestApp();
        using var socket = await ConnectAsync(app);

        await SendTextAsync(socket, "Created via WebSocket");

        var taskEvent = await ReceiveEventAsync(socket);
        Assert.Equal(new TaskEvent.Created(new TaskItem(1, "Created via WebSocket")), taskEvent);
    }

    [Fact]
    public async Task REST_delete_is_pushed_to_WebSocket_clients()
    {
        await using var app = new TestApp();
        var client = app.CreateClient();
        using var socket = await ConnectAsync(app);

        // Creating a task first guarantees the socket is subscribed before we delete.
        await SendTextAsync(socket, "Delete me");
        var created = Assert.IsType<TaskEvent.Created>(await ReceiveEventAsync(socket));

        await client.DeleteAsync($"/tasks/{created.Task.Id}");

        Assert.Equal(new TaskEvent.Deleted(created.Task.Id), await ReceiveEventAsync(socket));
    }
}
