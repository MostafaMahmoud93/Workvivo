namespace Workvivo.Domain.Exceptions;

/// <summary>
/// One or more inputs failed validation. Carries the per-field errors so the API
/// can return the standard ProblemDetails <c>errors</c> dictionary and the Angular
/// form can attach each message to the right control.
/// </summary>
public sealed class ValidationException : AppException
{
    private static readonly IReadOnlyDictionary<string, string[]> Empty =
        new Dictionary<string, string[]>();

    public ValidationException()
        : base("One or more validation errors occurred.")
    {
        Errors = Empty;
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]> { [propertyName] = [errorMessage] };
    }

    /// <summary>Property name (camelCased by the API) to its failure messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorType => "validation";
}
