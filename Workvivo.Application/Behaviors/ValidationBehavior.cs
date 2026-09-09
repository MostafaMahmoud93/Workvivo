using FluentValidation;
using MediatR;
using ValidationException = Workvivo.Domain.Exceptions.ValidationException;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator registered for the request before the
/// handler sees it.
///
/// Centralising this is what lets handlers open with the real work instead of a
/// screen of argument checks, and guarantees a validator cannot be silently skipped
/// because a controller forgot to call it.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next();
        }

        // camelCase the property names so they line up with the JSON the client sent
        // and with the Angular reactive-form control names.
        var errors = failures
            .GroupBy(f => ToCamelCase(f.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        throw new ValidationException(errors);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        // Nested paths arrive as "Options[0].Text"; only the leading segment of each
        // dotted part needs lowering.
        var parts = propertyName.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0 && char.IsUpper(parts[i][0]))
            {
                parts[i] = char.ToLowerInvariant(parts[i][0]) + parts[i][1..];
            }
        }

        return string.Join('.', parts);
    }
}
