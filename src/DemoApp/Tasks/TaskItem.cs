namespace DemoApp.Tasks;

// Records give us value equality, immutability and `with` copies for free.
// (Named TaskItem because `Task` is already taken by System.Threading.Tasks.)

/// <summary>A single to-do item.</summary>
public record TaskItem(long Id, string Title, bool Done = false);

/// <summary>Body of POST /tasks.</summary>
public record CreateTaskRequest(string Title);

/// <summary>Body of PATCH /tasks/{id}. Only fields that are set get updated.</summary>
public record UpdateTaskRequest(string? Title = null, bool? Done = null);
