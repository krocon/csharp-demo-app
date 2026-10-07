using System.Net;
using System.Net.Http.Json;
using DemoApp.Setup;
using DemoApp.Tasks;

namespace DemoApp.Tests.Tasks;

public class TaskRoutesTests
{
    /// <summary>Starts the app (empty repository unless given) and returns an HTTP client for it.</summary>
    private static HttpClient Setup(ITaskRepository? repository = null) =>
        new TestApp(repository).CreateClient();

    [Fact]
    public async Task Root_returns_hello()
    {
        var client = Setup();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello, ASP.NET Core!", body);
        Assert.Contains("href=\"/live/\"", body);
    }

    [Fact]
    public async Task Health_check_is_UP()
    {
        var client = Setup();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal(new Dictionary<string, string> { ["status"] = "UP" }, body);
    }

    [Fact]
    public async Task Create_and_fetch_a_task()
    {
        var client = Setup();

        var created = await client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/tasks/1", created.Headers.Location?.ToString());

        var task = await client.GetFromJsonAsync<TaskItem>("/tasks/1");
        Assert.Equal(new TaskItem(1, "Write tests", Done: false), task);
    }

    [Fact]
    public async Task List_tasks_filtered_by_done()
    {
        var repository = new InMemoryTaskRepository();
        await repository.CreateAsync("Open task");
        var closed = await repository.CreateAsync("Closed task");
        await repository.UpdateAsync(closed.Id, new UpdateTaskRequest(Done: true));
        var client = Setup(repository);

        var doneTasks = await client.GetFromJsonAsync<List<TaskItem>>("/tasks?done=true");

        Assert.Equal(new[] { "Closed task" }, doneTasks!.Select(task => task.Title));
    }

    [Fact]
    public async Task Patch_marks_a_task_as_done()
    {
        var repository = new InMemoryTaskRepository();
        var task = await repository.CreateAsync("Ship it");
        var client = Setup(repository);

        var response = await client.PatchAsJsonAsync($"/tasks/{task.Id}", new UpdateTaskRequest(Done: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<TaskItem>())!.Done);
    }

    [Fact]
    public async Task Delete_removes_a_task()
    {
        var repository = new InMemoryTaskRepository();
        var task = await repository.CreateAsync("Temporary");
        var client = Setup(repository);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task Blank_title_is_rejected()
    {
        var client = Setup();

        var response = await client.PostAsJsonAsync("/tasks", new CreateTaskRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("title must not be blank", error!.Message);
    }

    [Fact]
    public async Task Unknown_task_returns_404()
    {
        var client = Setup();

        var response = await client.GetAsync("/tasks/42");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Task 42 not found", error!.Message);
    }

    [Fact]
    public async Task Non_numeric_id_returns_400()
    {
        var client = Setup();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/tasks/abc")).StatusCode);
    }
}
