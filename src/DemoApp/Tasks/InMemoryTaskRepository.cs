using System.Collections.Concurrent;

namespace DemoApp.Tasks;

/// <summary>Thread-safe, in-memory implementation. Data is lost on restart.</summary>
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<long, TaskItem> _tasks = new();
    private long _lastId;

    public Task<IReadOnlyList<TaskItem>> FindAllAsync(bool? done = null)
    {
        IReadOnlyList<TaskItem> result = _tasks.Values
            .Where(task => done is null || task.Done == done)
            .OrderBy(task => task.Id)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<TaskItem?> FindByIdAsync(long id) =>
        Task.FromResult(_tasks.GetValueOrDefault(id));

    public Task<TaskItem> CreateAsync(string title) => Task.FromResult(Add(title));

    private TaskItem Add(string title)
    {
        var task = new TaskItem(Interlocked.Increment(ref _lastId), title.Trim());
        _tasks[task.Id] = task;
        return task;
    }

    public Task<TaskItem?> UpdateAsync(long id, UpdateTaskRequest request)
    {
        // Optimistic concurrency: only replace the task if nobody changed it in between.
        while (_tasks.TryGetValue(id, out var existing))
        {
            var updated = existing with
            {
                Title = request.Title?.Trim() ?? existing.Title,
                Done = request.Done ?? existing.Done,
            };
            if (_tasks.TryUpdate(id, updated, existing))
            {
                return Task.FromResult<TaskItem?>(updated);
            }
        }
        return Task.FromResult<TaskItem?>(null);
    }

    public Task<bool> DeleteAsync(long id) => Task.FromResult(_tasks.TryRemove(id, out _));

    /// <summary>A repository pre-filled with a few tasks, handy for demos.</summary>
    public static InMemoryTaskRepository WithSampleData()
    {
        var repository = new InMemoryTaskRepository();
        repository.Add("Learn C#");
        repository.Add("Build an ASP.NET Core API");
        repository.Add("Record a one-minute video");
        return repository;
    }
}
