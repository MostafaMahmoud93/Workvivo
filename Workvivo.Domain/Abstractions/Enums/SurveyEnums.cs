namespace Workvivo.Domain.Abstractions.Enums;

public enum SurveyStatus
{
    Draft = 0,
    Scheduled = 1,
    Published = 2,
    Closed = 3,
    Archived = 4,
}

/// <summary>
/// Question shapes the survey builder offers.
///
/// Each decides which column on an answer row carries the value: choice questions
/// use the option foreign key, text uses the text column, and everything scalar
/// (rating, scale, NPS) uses the numeric column with its own min and max.
/// </summary>
public enum SurveyQuestionType
{
    SingleChoice = 0,
    MultipleChoice = 1,
    Rating = 2,
    Text = 3,
    YesNo = 4,

    /// <summary>Net Promoter Score - fixed 0 to 10 scale.</summary>
    Nps = 5,

    /// <summary>Arbitrary numeric scale with configurable bounds.</summary>
    Scale = 6,
}
