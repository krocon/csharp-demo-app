using DemoApp.Setup;
using DemoApp.Tasks;

// Entry point: builds and starts the web server (Kestrel).
// The port can be overridden with the PORT environment variable.
var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Dependency injection: one shared repository and event bus for the whole app.
// Tests replace them with fresh instances (see tests/DemoApp.Tests/TestApp.cs).
builder.Services.AddSingleton<ITaskRepository>(InMemoryTaskRepository.WithSampleData());
builder.Services.AddSingleton<TaskEventBus>();

builder.Services.AddMonitoring();
builder.Services.AddSerialization();
builder.Services.AddErrorHandling();

var app = builder.Build();

app.UseMonitoring();
app.UseErrorHandling();
app.UseSockets();
app.MapRoutes();

app.Run();

/// <summary>Makes the generated Program class visible to WebApplicationFactory in the tests.</summary>
public partial class Program;
