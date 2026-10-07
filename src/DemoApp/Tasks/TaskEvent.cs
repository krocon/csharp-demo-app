using System.Text.Json;
using System.Text.Json.Serialization;

namespace DemoApp.Tasks;

/// <summary>
/// Something that happened to a task. Sent to live clients as JSON, e.g.
/// {"type":"created","task":{"id":4,"title":"Try ASP.NET Core","done":false}}
/// The "type" discriminator is written by System.Text.Json's polymorphism support.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Created), "created")]
[JsonDerivedType(typeof(Updated), "updated")]
[JsonDerivedType(typeof(Deleted), "deleted")]
public abstract record TaskEvent
{
    public sealed record Created(TaskItem Task) : TaskEvent;

    public sealed record Updated(TaskItem Task) : TaskEvent;

    public sealed record Deleted(long Id) : TaskEvent;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Name of the event as used in the "type" field and as SSE event name.</summary>
    [JsonIgnore]
    public string Type => this switch
    {
        Created => "created",
        Updated => "updated",
        Deleted => "deleted",
        _ => throw new InvalidOperationException($"Unknown event {GetType().Name}"),
    };

    // Serialize as the base type, otherwise the "type" discriminator is left out.
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static TaskEvent FromJson(string json) =>
        JsonSerializer.Deserialize<TaskEvent>(json, JsonOptions)
        ?? throw new JsonException("Empty event");
}
