using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Surveys.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Surveys;

namespace Workvivo.Application.Features.Surveys.Queries.GetSurveys;

/// <summary>
/// Surveys the caller has been invited to.
///
/// Audience-filtered with the same key set as the feed and the polls, so "invited"
/// means one thing across the product.
/// </summary>
public sealed record GetSurveysQuery(bool IncludeClosed = false) : IQuery<IReadOnlyList<SurveySummaryDto>>;

public sealed class GetSurveysQueryHandler
    : IRequestHandler<GetSurveysQuery, IReadOnlyList<SurveySummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IAnonymityHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public GetSurveysQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IAnonymityHasher hasher,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SurveySummaryDto>> Handle(
        GetSurveysQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);
        var now = _clock.UtcNow;

        var query = _unitOfWork.Repository<Survey, Guid>()
            .GetAllQ()
            .Where(survey => survey.Status == SurveyStatus.Published || survey.Status == SurveyStatus.Closed)
            .Where(survey => _unitOfWork.Repository<SurveyAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Survey_Id == survey.Id && keys.Contains(audience.Audience_Key)));

        if (!request.IncludeClosed)
        {
            query = query.Where(survey =>
                survey.Status == SurveyStatus.Published
                && (survey.Start_Date == null || survey.Start_Date <= now)
                && (survey.End_Date == null || survey.End_Date > now));
        }

        var surveys = await query
            .OrderByDescending(survey => survey.Create_Date)
            .Select(survey => new
            {
                survey.Id,
                survey.Title_Ar,
                survey.Title_En,
                survey.Description_Ar,
                survey.Description_En,
                survey.Status,
                survey.Is_Anonymous,
                survey.Start_Date,
                survey.End_Date,
                survey.Allow_Multiple_Responses,
                QuestionCount = survey.Questions.Count(question => !question.Is_Deleted),
            })
            .ToListAsync(cancellationToken);

        if (surveys.Count == 0)
        {
            return [];
        }

        var answered = await AnsweredSurveyIdsAsync(
            surveys.ConvertAll(survey => (survey.Id, survey.Is_Anonymous)), employeeId, cancellationToken);

        return
        [
            .. surveys.Select(survey =>
            {
                var hasResponded = answered.Contains(survey.Id);

                var isOpen = survey.Status == SurveyStatus.Published
                    && (survey.Start_Date is null || survey.Start_Date <= now)
                    && (survey.End_Date is null || survey.End_Date > now);

                return new SurveySummaryDto
                {
                    Id = survey.Id,
                    Title = LocalizedText.Pick(survey.Title_Ar, survey.Title_En) ?? string.Empty,
                    Description = LocalizedText.Pick(survey.Description_Ar, survey.Description_En),
                    Status = (int)survey.Status,
                    IsAnonymous = survey.Is_Anonymous,
                    EndDate = survey.End_Date,
                    HasResponded = hasResponded,
                    CanRespond = isOpen && (!hasResponded || survey.Allow_Multiple_Responses),
                    QuestionCount = survey.QuestionCount,
                };
            }),
        ];
    }

    /// <summary>
    /// Which of these surveys the caller has already completed.
    ///
    /// Named and anonymous surveys are matched differently - by employee id and by
    /// keyed hash - but both have to be answered, or an anonymous survey would invite
    /// the same person again every time they opened the list.
    /// </summary>
    private async Task<HashSet<Guid>> AnsweredSurveyIdsAsync(
        List<(Guid Id, bool IsAnonymous)> surveys,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var named = surveys.FindAll(survey => !survey.IsAnonymous).ConvertAll(survey => survey.Id);
        var anonymous = surveys.FindAll(survey => survey.IsAnonymous);

        var responses = _unitOfWork.Repository<SurveyResponse, Guid>();
        var answered = new HashSet<Guid>();

        if (named.Count > 0)
        {
            answered.UnionWith(await responses.GetAllQ()
                .Where(response =>
                    named.Contains(response.Survey_Id)
                    && response.Employee_Id == employeeId
                    && response.Is_Complete)
                .Select(response => response.Survey_Id)
                .ToListAsync(cancellationToken));
        }

        if (anonymous.Count > 0)
        {
            var hashes = anonymous.ConvertAll(survey => _hasher.Hash(survey.Id, employeeId));

            answered.UnionWith(await responses.GetAllQ()
                .Where(response =>
                    response.Respondent_Hash != null
                    && hashes.Contains(response.Respondent_Hash)
                    && response.Is_Complete)
                .Select(response => response.Survey_Id)
                .ToListAsync(cancellationToken));
        }

        return answered;
    }
}
