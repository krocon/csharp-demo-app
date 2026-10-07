using DemoApp.Setup;

namespace DemoApp.Tasks;

/// <summary>
/// REST endpoints for tasks. Every change is published to the <see cref="TaskEventBus"/>:
///
///   GET    /tasks            list all tasks (optional ?done=true|false)
///   GET    /tasks/{id}       get one task
///   POST   /tasks            create a task
///   PATCH  /tasks/{id}       update title and/or done
///   DELETE /tasks/{id}       delete a task
///
/// Parameters are bound automatically: <c>long id</c> from the route, <c>bool? done</c>
/// from the query string, request records from the JSON body, services from DI.
/// Values that cannot be parsed (e.g. /tasks/abc) are answered with 400.
/// </summary>
public static class TaskRoutes
{
    public static IEndpointRouteBuilder MapTaskRoutes(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/tasks");

        tasks.MapGet("/", async (bool? done, ITaskRepository repository) =>
            await repository.FindAllAsync(done));

        tasks.MapGet("/{id}", async (long id, ITaskRepository repository) =>
            await repository.FindByIdAsync(id) ?? throw NotFound(id));

        tasks.MapPost("/", async (CreateTaskRequest request, ITaskRepository repository, TaskEventBus eventBus) =>
            {
                var task = await repository.CreateAsync(request.Title);
                eventBus.Publish(new TaskEvent.Created(task));
                return TypedResults.Created($"/tasks/{task.Id}", task);
            })
            .Validate<CreateTaskRequest>(TaskValidation.Validate);

        tasks.MapPatch("/{id}", async (long id, UpdateTaskRequest request, ITaskRepository repository, TaskEventBus eventBus) =>
            {
                var task = await repository.UpdateAsync(id, request) ?? throw NotFound(id);
                eventBus.Publish(new TaskEvent.Updated(task));
                return task;
            })
            .Validate<UpdateTaskRequest>(TaskValidation.Validate);

        tasks.MapDelete("/{id}", async (long id, ITaskRepository repository, TaskEventBus eventBus) =>
        {
            if (!await repository.DeleteAsync(id)) throw NotFound(id);
            eventBus.Publish(new TaskEvent.Deleted(id));
            return TypedResults.NoContent();
        });

        return app;
    }

    private static NotFoundException NotFound(long id) => new($"Task {id} not found");
}
