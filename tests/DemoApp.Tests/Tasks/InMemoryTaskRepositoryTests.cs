using DemoApp.Tasks;

namespace DemoApp.Tests.Tasks;

public class InMemoryTaskRepositoryTests
{
    // xUnit creates a new instance of the test class for every test: a fresh repository each time.
    private readonly InMemoryTaskRepository _repository = new();

    [Fact]
    public async Task Ids_are_assigned_sequentially()
    {
        var first = await _repository.CreateAsync("First");
        var second = await _repository.CreateAsync("Second");

        Assert.Equal(1L, first.Id);
        Assert.Equal(2L, second.Id);
    }

    [Fact]
    public async Task Update_only_changes_the_given_fields()
    {
        var task = await _repository.CreateAsync("Original");

        var updated = await _repository.UpdateAsync(task.Id, new UpdateTaskRequest(Done: true));

        Assert.Equal(new TaskItem(task.Id, "Original", Done: true), updated);
    }

    [Fact]
    public async Task Update_of_unknown_id_returns_null()
    {
        Assert.Null(await _repository.UpdateAsync(99, new UpdateTaskRequest(Title: "Nope")));
    }

    [Fact]
    public async Task Delete_returns_whether_a_task_was_removed()
    {
        var task = await _repository.CreateAsync("To delete");

        Assert.True(await _repository.DeleteAsync(task.Id));
        Assert.False(await _repository.DeleteAsync(task.Id));
    }
}
