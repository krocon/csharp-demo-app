using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DemoApp.Tasks;

/// <summary>
/// A tiny in-process pub/sub: routes publish events,
/// every connected WebSocket / SSE client reads them from its own Channel.
/// </summary>
public class TaskEventBus
{
    private readonly ConcurrentDictionary<Guid, Channel<TaskEvent>> _subscribers = new();

    public void Publish(TaskEvent taskEvent)
    {
        foreach (var channel in _subscribers.Values)
        {
            channel.Writer.TryWrite(taskEvent);
        }
    }

    /// <summary>
    /// Subscribes immediately and returns the subscriber's own reader.
    /// The subscription ends when <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public ChannelReader<TaskEvent> Subscribe(CancellationToken cancellationToken)
    {
        // Bounded, so a slow client can never eat all the memory: it just misses old events.
        var channel = Channel.CreateBounded<TaskEvent>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });
        var id = Guid.NewGuid();
        _subscribers[id] = channel;

        cancellationToken.Register(() =>
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }
}
