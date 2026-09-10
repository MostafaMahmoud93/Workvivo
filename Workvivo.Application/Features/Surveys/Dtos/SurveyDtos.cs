namespace Workvivo.Application.Features.Surveys.Dtos;

public sealed class SurveyQuestionOptionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class SurveyQuestionDto
{
    public Guid Id { get; init; }
    public int QuestionType { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? HelpText { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
    public int? MinValue { get; init; }
    public int? MaxValue { get; init; }
    public string? MinLabel { get; init; }
    public string? MaxLabel { get; init; }
    public IReadOnlyList<SurveyQuestionOptionDto> Options { get; init; } = [];
}

public class SurveySummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Status { get; init; }
    public bool IsAnonymous { get; init; }
    public DateTime? EndDate { get; init; }
    public bool HasResponded { get; init; }
    public bool CanRespond { get; init; }
    public int QuestionCount { get; init; }
}

public sealed class SurveyDetailDto : SurveySummaryDto
{
    public IReadOnlyList<SurveyQuestionDto> Questions { get; init; } = [];
}

/// <summary>Aggregated answers for one question.</summary>
public sealed class SurveyQuestionResultDto
{
    public Guid QuestionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int QuestionType { get; init; }
    public int AnswerCount { get; init; }

    /// <summary>Counts per option, for choice questions.</summary>
    public IReadOnlyList<SurveyOptionResultDto> Options { get; init; } = [];

    /// <summary>Mean, for Rating, Scale, NPS and Yes/No.</summary>
    public double? Average { get; init; }

    /// <summary>
    /// Free-text answers, released only above a minimum response count.
    ///
    /// A verbatim comment is identifying even on an anonymous survey - the phrasing,
    /// the incident described, the team it concerns. Below the threshold they are
    /// withheld entirely rather than shown to a manager who can work out who wrote
    /// them.
    /// </summary>
    public IReadOnlyList<string> TextAnswers { get; init; } = [];

    /// <summary>True when text answers were withheld because too few people answered.</summary>
    public bool TextSuppressed { get; init; }
}

public sealed class SurveyOptionResultDto
{
    public Guid OptionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class SurveyResultsDto
{
    public Guid SurveyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsAnonymous { get; init; }
    public int ResponseCount { get; init; }
    public int InvitedCount { get; init; }
    public IReadOnlyList<SurveyQuestionResultDto> Questions { get; init; } = [];
}
