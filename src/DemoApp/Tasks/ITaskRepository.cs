namespace DemoApp.Tasks;

/// <summary>
/// Storage abstraction for tasks.
/// Swap the implementation (e.g. for EF Core and a database) without touching the routes.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> FindAllAsync(bool? done = null);
    Task<TaskItem?> FindByIdAsync(long id);
    Task<TaskItem> CreateAsync(string title);
    Task<TaskItem?> UpdateAsync(long id, UpdateTaskRequest request);
    Task<bool> DeleteAsync(long id);
}
