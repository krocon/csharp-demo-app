namespace DemoApp.Setup;

/// <summary>
/// Converts request and response bodies to and from JSON (System.Text.Json).
/// Minimal APIs already use web defaults: camelCase names, case-insensitive
/// reading, unknown properties are ignored.
/// </summary>
public static class Serialization
{
    public static IServiceCollection AddSerialization(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.WriteIndented = true;
        });
}
