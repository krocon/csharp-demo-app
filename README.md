# C# + ASP.NET Core Demo App

A small, self-contained REST API built with [ASP.NET Core](https://learn.microsoft.com/aspnet/core)
Minimal APIs and C#. It is the reference example for a series of one-minute videos — every file
focuses on **one** concept, so each video can point to exactly one place in the code.

It is the C# counterpart of [kotlin-ktor-demo-app](https://github.com/krocon/kotlin-ktor-demo-app):
same endpoints, same JSON, same live demo page.

## Quick start

Requirements: .NET SDK 10 or newer. No extra NuGet packages are needed for the app itself,
everything ships with ASP.NET Core.

```bash
dotnet run --project src/DemoApp     # start the server on http://localhost:8080
dotnet test                          # run all tests
dotnet publish src/DemoApp -c Release -o out   # build a runnable app in out/
```

In JetBrains Rider: open `CSharpDemoApp.sln` and run the **DemoApp** configuration — it opens the
live demo page in your browser.

Try it:

```bash
curl http://localhost:8080/tasks
curl -X POST http://localhost:8080/tasks -H "Content-Type: application/json" -d '{"title":"Try ASP.NET Core"}'
```

Or open [`requests.http`](requests.http) in Rider and click the ▶ icons.

## Endpoints

| Method | Path            | Description                              |
|--------|-----------------|------------------------------------------|
| GET    | `/`             | Hello world                              |
| GET    | `/health`       | Health check                             |
| GET    | `/tasks`        | List tasks (optional `?done=true/false`) |
| GET    | `/tasks/{id}`   | Get one task                             |
| POST   | `/tasks`        | Create a task                            |
| PATCH  | `/tasks/{id}`   | Update title and/or done                 |
| DELETE | `/tasks/{id}`   | Delete a task                            |
| WS     | `/ws/tasks`     | Live task events; send text to create    |
| GET    | `/tasks/events` | Live task events as Server-Sent Events   |
| GET    | `/live/`        | Browser demo page for the live features  |

Errors always come back as JSON with the matching status code:

```json
{ "message": "Task 42 not found" }
```

## Live updates (WebSockets & SSE)

Open <http://localhost:8080/live/> in two browser windows. Add, check or delete a task in one
window – the other one updates instantly.

Every change made through the REST API or the WebSocket is published to a `TaskEventBus`
(one `System.Threading.Channels` channel per subscriber) and pushed to all connected clients as JSON:

```json
{"type":"created","task":{"id":4,"title":"Try ASP.NET Core","done":false}}
```

From the terminal:

```bash
curl -N http://localhost:8080/tasks/events   # SSE stream, keep open
websocat ws://localhost:8080/ws/tasks          # WebSocket, type a title + Enter
```

## Project map (one topic per file)

| Topic                              | File                                                                                       |
|------------------------------------|--------------------------------------------------------------------------------------------|
| Starting the server, DI            | [`Program.cs`](src/DemoApp/Program.cs)                                                     |
| Routing basics                     | [`Setup/Routing.cs`](src/DemoApp/Setup/Routing.cs)                                         |
| REST routes / CRUD (Minimal APIs)  | [`Tasks/TaskRoutes.cs`](src/DemoApp/Tasks/TaskRoutes.cs)                                   |
| JSON with System.Text.Json         | [`Setup/Serialization.cs`](src/DemoApp/Setup/Serialization.cs)                             |
| Records / DTOs                     | [`Tasks/TaskItem.cs`](src/DemoApp/Tasks/TaskItem.cs)                                       |
| Error handling (`IExceptionHandler`) | [`Setup/ErrorHandling.cs`](src/DemoApp/Setup/ErrorHandling.cs)                           |
| Request validation (endpoint filter) | [`Setup/Validation.cs`](src/DemoApp/Setup/Validation.cs)                                 |
| WebSockets & SSE setup             | [`Setup/Sockets.cs`](src/DemoApp/Setup/Sockets.cs)                                         |
| Live endpoints (WS + SSE)          | [`Live/LiveRoutes.cs`](src/DemoApp/Live/LiveRoutes.cs)                                     |
| Pub/sub with Channels              | [`Tasks/TaskEventBus.cs`](src/DemoApp/Tasks/TaskEventBus.cs)                               |
| Polymorphic JSON events            | [`Tasks/TaskEvent.cs`](src/DemoApp/Tasks/TaskEvent.cs)                                     |
| Browser WebSocket client           | [`wwwroot/live/index.html`](src/DemoApp/wwwroot/live/index.html)                           |
| Testing WebSockets                 | [`LiveRoutesTests.cs`](tests/DemoApp.Tests/Live/LiveRoutesTests.cs)                        |
| Logging (HTTP logging)             | [`Setup/Monitoring.cs`](src/DemoApp/Setup/Monitoring.cs), [`appsettings.json`](src/DemoApp/appsettings.json) |
| Repository pattern                 | [`Tasks/ITaskRepository.cs`](src/DemoApp/Tasks/ITaskRepository.cs)                         |
| Thread-safe in-memory storage      | [`Tasks/InMemoryTaskRepository.cs`](src/DemoApp/Tasks/InMemoryTaskRepository.cs)           |
| Testing routes (`WebApplicationFactory`) | [`TestApp.cs`](tests/DemoApp.Tests/TestApp.cs), [`TaskRoutesTests.cs`](tests/DemoApp.Tests/Tasks/TaskRoutesTests.cs) |
| Unit testing (xUnit)               | [`InMemoryTaskRepositoryTests.cs`](tests/DemoApp.Tests/Tasks/InMemoryTaskRepositoryTests.cs) |
| Central package versions           | [`Directory.Packages.props`](Directory.Packages.props)                                     |
| Shared build settings              | [`Directory.Build.props`](Directory.Build.props)                                           |
| Docker                             | [`Dockerfile`](Dockerfile)                                                                 |

## Ktor → ASP.NET Core cheat sheet

| Ktor (Kotlin)                        | ASP.NET Core (C#)                                       |
|--------------------------------------|---------------------------------------------------------|
| `embeddedServer(Netty)`              | `WebApplication.CreateBuilder()` + Kestrel              |
| `install(Plugin)`                    | `builder.Services.AddX()` + `app.UseX()`                |
| `routing { route("/tasks") { … } }`  | `app.MapGroup("/tasks").MapGet(…)`                      |
| `data class`                         | `record`                                                |
| `sealed interface` + `@SerialName`   | `abstract record` + `[JsonDerivedType]`                 |
| `StatusPages`                        | `IExceptionHandler` + `UseExceptionHandler()`           |
| `RequestValidation`                  | Endpoint filter (`AddEndpointFilter`)                   |
| `SharedFlow`                         | `Channel<T>` per subscriber                             |
| `CallLogging`                        | `AddHttpLogging()` / `UseHttpLogging()`                 |
| `testApplication { }`                | `WebApplicationFactory<Program>`                        |
| `libs.versions.toml`                 | `Directory.Packages.props`                              |

## Docker

```bash
docker build -t aspnet-demo .
docker run -p 8080:8080 aspnet-demo
```

## Configuration

| Variable | Default | Description      |
|----------|---------|------------------|
| `PORT`   | `8080`  | HTTP server port |
