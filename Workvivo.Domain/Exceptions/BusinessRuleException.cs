namespace Workvivo.Domain.Exceptions;

/// <summary>
/// The request was well formed and the caller was allowed to make it, but a domain
/// rule refuses it - replying to a comment already two levels deep, voting in a
/// closed poll, recognising yourself.
///
/// 422 rather than 400: the syntax was fine, the semantics were not.
/// </summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? ruleCode = null)
        : base(message)
    {
        RuleCode = ruleCode;
    }

    /// <summary>Stable identifier so the client can localise or branch on the rule.</summary>
    public string? RuleCode { get; }

    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;

    public override string ErrorType => "business-rule";

    public override IReadOnlyDictionary<string, object?>? Extensions =>
        RuleCode is null ? null : new Dictionary<string, object?> { ["ruleCode"] = RuleCode };
}
