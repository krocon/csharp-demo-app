using DemoApp.Tasks;

namespace DemoApp.Setup;

/// <summary>
/// Validates request bodies right after they are bound, before the handler runs.
/// Implemented as an endpoint filter: <c>.Validate&lt;CreateTaskRequest&gt;(TaskValidation.Validate)</c>.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Runs <paramref name="validate"/> on the first argument of type <typeparamref name="T"/>.
    /// A non-null result is the error message and stops the request with a 400.
    /// </summary>
    public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder builder, Func<T, string?> validate) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var request = context.Arguments.OfType<T>().FirstOrDefault();
            if (request is not null && validate(request) is { } error)
            {
                throw new ValidationException(error);
            }
            return await next(context);
        });
}

/// <summary>The actual rules for task requests.</summary>
public static class TaskValidation
{
    private const int MaxTitleLength = 100;

    public static string? Validate(CreateTaskRequest request) => ValidateTitle(request.Title);

    public static string? Validate(UpdateTaskRequest request) =>
        request.Title is null ? null : ValidateTitle(request.Title);

    private static string? ValidateTitle(string? title) => title switch
    {
        _ when string.IsNullOrWhiteSpace(title) => "title must not be blank",
        { Length: > MaxTitleLength } => $"title must be at most {MaxTitleLength} characters",
        _ => null,
    };
}
