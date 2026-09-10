using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Surveys.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Surveys;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Surveys.Queries.GetSurveyResults;

/// <summary>
/// Aggregated survey results.
///
/// Aggregated, never itemised. There is no endpoint anywhere that returns individual
/// responses, and that is deliberate: an anonymous survey whose raw rows can be read
/// by a manager is not anonymous, and people work that out quickly - after which the
/// answers stop being worth collecting.
/// </summary>
public sealed record GetSurveyResultsQuery(Guid SurveyId) : IQuery<SurveyResultsDto>;

public sealed class GetSurveyResultsQueryHandler
    : IRequestHandler<GetSurveyResultsQuery, SurveyResultsDto>
{
    /// <summary>
    /// Fewest responses before free-text answers are shown.
    ///
    /// A verbatim comment identifies its author far more often than a tick-box does -
    /// the phrasing, the incident, the team it names. Below this the comments are
    /// withheld entirely; the counts still appear, because a count of four cannot be
    /// traced to anybody.
    /// </summary>
    public const int MinimumResponsesForVerbatims = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;

    public GetSurveyResultsQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
    }

    public async Task<SurveyResultsDto> Handle(
        GetSurveyResultsQuery request,
        CancellationToken cancellationToken)
    {
        await _currentEmployee.RequireIdAsync(cancellationToken);

        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.SurveyManage, cancellationToken))
        {
            throw new ForbiddenException("You cannot read survey results.", PermissionKeys.SurveyManage);
        }

        var survey = await _unitOfWork.Repository<Survey, Guid>()
            .GetAllQ()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SurveyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Survey), request.SurveyId);

        var questions = await _unitOfWork.Repository<SurveyQuestion, Guid>()
            .GetAllQ()
            .Where(question => question.Survey_Id == survey.Id)
            .OrderBy(question => question.Sort_Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var questionIds = questions.ConvertAll(question => question.Id);

        var options = await _unitOfWork.Repository<SurveyQuestionOption, Guid>()
            .GetAllQ()
            .Where(option => questionIds.Contains(option.Question_Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Completed responses only. Counting partial saves would inflate the
        // participation rate with people who opened the survey and walked away.
        var completedIds = _unitOfWork.Repository<SurveyResponse, Guid>()
            .GetAllQ()
            .Where(response => response.Survey_Id == survey.Id && response.Is_Complete)
            .Select(response => response.Id);

        var answers = _unitOfWork.Repository<SurveyAnswer, Guid>()
            .GetAllQ()
            .Where(answer => completedIds.Contains(answer.Response_Id));

        // Three aggregates, computed in the database. Pulling every answer row back
        // to count them in memory would be the whole response table for a
        // company-wide survey.
        var optionCounts = await answers
            .Where(answer => answer.Option_Id != null)
            .GroupBy(answer => new { answer.Question_Id, answer.Option_Id })
            .Select(group => new
            {
                group.Key.Question_Id,
                group.Key.Option_Id,
                Count = group.Count(),
            })
            .ToListAsync(cancellationToken);

        var numericStats = await answers
            .Where(answer => answer.Numeric_Value != null)
            .GroupBy(answer => answer.Question_Id)
            .Select(group => new
            {
                QuestionId = group.Key,
                Average = group.Average(answer => (double)answer.Numeric_Value!.Value),
                Count = group.Count(),
            })
            .ToListAsync(cancellationToken);

        var responseCount = await _unitOfWork.Repository<SurveyResponse, Guid>()
            .GetAllQ()
            .CountAsync(
                response => response.Survey_Id == survey.Id && response.Is_Complete,
                cancellationToken);

        var releaseVerbatims = responseCount >= MinimumResponsesForVerbatims;

        var textAnswers = releaseVerbatims
            ? await answers
                .Where(answer => answer.Text_Value != null && answer.Text_Value != "")
                .Select(answer => new { answer.Question_Id, answer.Text_Value })
                .Take(1000)
                .ToListAsync(cancellationToken)
            : [];

        var textCounts = await answers
            .Where(answer => answer.Text_Value != null && answer.Text_Value != "")
            .GroupBy(answer => answer.Question_Id)
            .Select(group => new { QuestionId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new SurveyResultsDto
        {
            SurveyId = survey.Id,
            Title = LocalizedText.Pick(survey.Title_Ar, survey.Title_En) ?? string.Empty,
            IsAnonymous = survey.Is_Anonymous,
            ResponseCount = responseCount,
            InvitedCount = survey.Invited_Count,
            Questions =
            [
                .. questions.Select(question =>
                {
                    var isText = question.Question_Type == SurveyQuestionType.Text;

                    var numeric = numericStats.Find(stat => stat.QuestionId == question.Id);
                    var text = textCounts.Find(stat => stat.QuestionId == question.Id);

                    var perOption = optionCounts.FindAll(count => count.Question_Id == question.Id);

                    return new SurveyQuestionResultDto
                    {
                        QuestionId = question.Id,
                        Text = LocalizedText.Pick(question.Text_Ar, question.Text_En) ?? string.Empty,
                        QuestionType = (int)question.Question_Type,
                        AnswerCount = isText
                            ? text?.Count ?? 0
                            : numeric?.Count ?? perOption.Sum(count => count.Count),
                        Average = numeric?.Average,
                        Options =
                        [
                            .. options
                                .FindAll(option => option.Question_Id == question.Id)
                                .OrderBy(option => option.Sort_Order)
                                .Select(option => new SurveyOptionResultDto
                                {
                                    OptionId = option.Id,
                                    Text = LocalizedText.Pick(option.Text_Ar, option.Text_En) ?? string.Empty,
                                    Count = perOption
                                        .Find(count => count.Option_Id == option.Id)?.Count ?? 0,
                                }),
                        ],
                        TextAnswers = isText && releaseVerbatims
                            ?
                            [
                                .. textAnswers
                                    .FindAll(answer => answer.Question_Id == question.Id)
                                    .Select(answer => answer.Text_Value!),
                            ]
                            : [],
                        TextSuppressed = isText && !releaseVerbatims && (text?.Count ?? 0) > 0,
                    };
                }),
            ],
        };
    }
}
