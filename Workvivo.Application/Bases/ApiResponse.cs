namespace Workvivo.Application.Bases;

/// <summary>
/// Standard success envelope for every new endpoint.
///
/// Shape-compatible with the template's <see cref="ServiceResponse{T}"/> - it adds
/// <c>traceId</c> and nothing else - so the Angular <c>ServiceResponse&lt;T&gt;</c>
/// interface keeps working unchanged while new code gains a correlation handle.
/// Failures never use this type: they return RFC 7807 ProblemDetails carrying the
/// same <c>traceId</c>, so one identifier ties a client report to a server log line.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    /// <summary>Correlation id for this request. Quote it in a bug report.</summary>
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

/// <summary>Non-generic form for endpoints that return no payload.</summary>
public sealed class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "") =>
        new() { Success = true, Message = message };
}
