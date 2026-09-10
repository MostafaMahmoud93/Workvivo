using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.Surveys;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Surveys.Commands.SubmitSurveyResponse;

/// <summary>
/// One answer as the client sends it. Which field carries the value depends on the
/// question type; the handler checks that they agree.
/// </summary>
public sealed record SurveyAnswerInput(
    Guid QuestionId,
    IReadOnlyList<Guid>? OptionIds,
    string? TextValue,
    int? NumericValue);

public sealed record SubmitSurveyResponseCommand(
    Guid SurveyId,
    IReadOnlyList<SurveyAnswerInput> Answers) : ICommand<Guid>;

public sealed class SubmitSurveyResponseCommandValidator
    : AbstractValidator<SubmitSurveyResponseCommand>
{
    public SubmitSurveyResponseCommandValidator()
    {
        RuleFor(x => x.SurveyId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty().Must(answers => answers.Count <= 200);
    }
}

public sealed class SubmitSurveyResponseCommandHandler
    : IRequestHandler<SubmitSurveyResponseCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IAnonymityHasher _hasher;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public SubmitSurveyResponseCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IAnonymityHasher hasher,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _hasher = hasher;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(
        SubmitSurveyResponseCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var survey = await _unitOfWork.Repository<Survey, Guid>().FindByIDAsync(request.SurveyId)
            ?? throw new NotFoundException(nameof(Survey), request.SurveyId);

        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var invited = await _unitOfWork.Repository<SurveyAudience, Guid>()
            .GetAllQ()
            .AnyAsync(
                audience => audience.Survey_Id == survey.Id && keys.Contains(audience.Audience_Key),
                cancellationToken);

        if (!invited)
        {
            throw new NotFoundException(nameof(Survey), request.SurveyId);
        }

        if (!survey.IsOpenAt(now))
        {
            throw new BusinessRuleException("This survey is not open.", "survey.closed");
        }

        var hash = survey.Is_Anonymous ? _hasher.Hash(survey.Id, employeeId) : null;

        var responses = _unitOfWork.Repository<SurveyResponse, Guid>();

        var alreadyResponded = await responses.GetAllQ()
            .AnyAsync(
                response => response.Survey_Id == survey.Id
                    && response.Is_Complete
                    && (survey.Is_Anonymous
                        ? response.Respondent_Hash == hash
                        : response.Employee_Id == employeeId),
                cancellationToken);

        if (alreadyResponded && !survey.Allow_Multiple_Responses)
        {
            throw new BusinessRuleException(
                "You have already answered this survey.", "survey.already-answered");
        }

        var questions = await _unitOfWork.Repository<SurveyQuestion, Guid>()
            .GetAllQ()
            .Where(question => question.Survey_Id == survey.Id)
            .ToListAsync(cancellationToken);

        var byId = questions.ToDictionary(question => question.Id);

        var answered = request.Answers
            .Where(answer => byId.ContainsKey(answer.QuestionId))
            .ToList();

        // Required questions are enforced here, not only in the client. A submission
        // assembled by hand would otherwise produce a "complete" response with the
        // uncomfortable questions missing.
        var missing = questions
            .Where(question => question.Is_Required)
            .Where(question => !answered.Exists(answer => answer.QuestionId == question.Id && HasValue(answer)))
            .ToList();

        if (missing.Count > 0)
        {
            throw new BusinessRuleException(
                $"{missing.Count} required question(s) have not been answered.",
                "survey.required-missing");
        }

        var profile = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new { employee.Department_Id, employee.Location_Id })
            .FirstOrDefaultAsync(cancellationToken);

        var response = new SurveyResponse
        {
            Id = Guid.NewGuid(),
            Survey_Id = survey.Id,

            // Exactly one of these. An anonymous response carrying the employee id
            // would not be anonymous whatever the survey claimed.
            Employee_Id = survey.Is_Anonymous ? null : employeeId,
            Respondent_Hash = hash,
            Started_At = now,
            Submitted_At = now,
            Is_Complete = true,

            // Captured now rather than joined later, so results for "Finance in March"
            // do not shift when somebody transfers out in April - and because an
            // anonymous response has no employee row to join to at all.
            Department_Id_At_Submission = profile?.Department_Id,
            Location_Id_At_Submission = profile?.Location_Id,
            Is_Deleted = false,
        };

        await responses.AddAsync(response);

        foreach (var answer in answered)
        {
            AddAnswers(response, byId[answer.QuestionId], answer);
        }

        await _unitOfWork.Repository<Survey, Guid>().ExecuteUpdateAsync(
            candidate => candidate.Id == survey.Id,
            setters => setters.SetProperty(
                candidate => candidate.Response_Count,
                candidate => candidate.Response_Count + 1),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response.Id;
    }

    private static bool HasValue(SurveyAnswerInput answer) =>
        answer.OptionIds?.Count > 0
        || answer.NumericValue is not null
        || !string.IsNullOrWhiteSpace(answer.TextValue);

    /// <summary>
    /// Writes the answer rows for one question.
    ///
    /// The value is taken from the field the question type expects, not from whatever
    /// the client happened to fill in. A rating question that accepted free text
    /// would poison every average computed over it.
    /// </summary>
    private void AddAnswers(SurveyResponse response, SurveyQuestion question, SurveyAnswerInput answer)
    {
        switch (question.Question_Type)
        {
            case SurveyQuestionType.SingleChoice:
            case SurveyQuestionType.MultipleChoice:
                var options = answer.OptionIds ?? [];

                // A single-choice question takes the first selection and ignores the
                // rest rather than failing: the extra values are a client bug, and
                // discarding a person's whole submission over it is worse.
                var chosen = question.Question_Type == SurveyQuestionType.SingleChoice
                    ? options.Take(1)
                    : options;

                foreach (var optionId in chosen.Distinct())
                {
                    response.Answers.Add(new SurveyAnswer
                    {
                        Id = Guid.NewGuid(),
                        Response_Id = response.Id,
                        Question_Id = question.Id,
                        Option_Id = optionId,
                        Is_Deleted = false,
                    });
                }

                break;

            case SurveyQuestionType.Text:
                if (!string.IsNullOrWhiteSpace(answer.TextValue))
                {
                    response.Answers.Add(new SurveyAnswer
                    {
                        Id = Guid.NewGuid(),
                        Response_Id = response.Id,
                        Question_Id = question.Id,

                        // Plain text. A verbatim comment is shown back to managers and
                        // exported; markup has no place in either.
                        Text_Value = _sanitizer.ToPlainText(answer.TextValue),
                        Is_Deleted = false,
                    });
                }

                break;

            default:
                if (answer.NumericValue is { } numeric)
                {
                    response.Answers.Add(new SurveyAnswer
                    {
                        Id = Guid.NewGuid(),
                        Response_Id = response.Id,
                        Question_Id = question.Id,
                        Numeric_Value = Clamp(question, numeric),
                        Is_Deleted = false,
                    });
                }

                break;
        }
    }

    /// <summary>
    /// Holds a numeric answer inside the question's declared range.
    ///
    /// Clamped rather than rejected: an out-of-range value is a client defect, and the
    /// harm of storing it - a skewed average that nobody can explain - is worse than
    /// the harm of nudging it to the boundary.
    /// </summary>
    private static int Clamp(SurveyQuestion question, int value) => question.Question_Type switch
    {
        SurveyQuestionType.Nps => Math.Clamp(value, 0, 10),
        SurveyQuestionType.YesNo => Math.Clamp(value, 0, 1),
        _ => Math.Clamp(value, question.Min_Value ?? int.MinValue, question.Max_Value ?? int.MaxValue),
    };
}
