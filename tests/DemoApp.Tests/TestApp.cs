using DemoApp.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DemoApp.Tests;

/// <summary>
/// Starts the whole app in memory (no real network port) via WebApplicationFactory.
/// Each instance gets its own repository and event bus, so tests never share state.
/// </summary>
public sealed class TestApp(ITaskRepository? repository = null) : WebApplicationFactory<Program>
{
    public ITaskRepository Repository { get; } = repository ?? new InMemoryTaskRepository();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Registered after Program.cs, so these win over the app's own singletons.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Repository);
            services.AddSingleton(new TaskEventBus());
        });
    }
}
