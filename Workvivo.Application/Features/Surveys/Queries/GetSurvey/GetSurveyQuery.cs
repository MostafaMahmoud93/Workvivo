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

namespace Workvivo.Application.Features.Surveys.Queries.GetSurvey;

/// <summary>One survey with its questions, ready to answer.</summary>
public sealed record GetSurveyQuery(Guid SurveyId) : IQuery<SurveyDetailDto>;

public sealed class GetSurveyQueryHandler : IRequestHandler<GetSurveyQuery, SurveyDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IAnonymityHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public GetSurveyQueryHandler(
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

    public async Task<SurveyDetailDto> Handle(GetSurveyQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);
        var now = _clock.UtcNow;

        var survey = await _unitOfWork.Repository<Survey, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Id == request.SurveyId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Survey), request.SurveyId);

        // Not addressed to this person is "not found", not "forbidden". A survey's
        // title alone can disclose a reorganisation or an investigation.
        var invited = await _unitOfWork.Repository<SurveyAudience, Guid>()
            .GetAllQ()
            .AnyAsync(
                audience => audience.Survey_Id == survey.Id && keys.Contains(audience.Audience_Key),
                cancellationToken);

        if (!invited || survey.Status == SurveyStatus.Draft)
        {
            throw new NotFoundException(nameof(Survey), request.SurveyId);
        }

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
            .OrderBy(option => option.Sort_Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hash = survey.Is_Anonymous ? _hasher.Hash(survey.Id, employeeId) : null;

        var hasResponded = await _unitOfWork.Repository<SurveyResponse, Guid>()
            .GetAllQ()
            .AnyAsync(
                response => response.Survey_Id == survey.Id
                    && response.Is_Complete
                    && (survey.Is_Anonymous
                        ? response.Respondent_Hash == hash
                        : response.Employee_Id == employeeId),
                cancellationToken);

        return new SurveyDetailDto
        {
            Id = survey.Id,
            Title = LocalizedText.Pick(survey.Title_Ar, survey.Title_En) ?? string.Empty,
            Description = LocalizedText.Pick(survey.Description_Ar, survey.Description_En),
            Status = (int)survey.Status,
            IsAnonymous = survey.Is_Anonymous,
            EndDate = survey.End_Date,
            HasResponded = hasResponded,
            CanRespond = survey.IsOpenAt(now) && (!hasResponded || survey.Allow_Multiple_Responses),
            QuestionCount = questions.Count,
            Questions =
            [
                .. questions.Select(question => new SurveyQuestionDto
                {
                    Id = question.Id,
                    QuestionType = (int)question.Question_Type,
                    Text = LocalizedText.Pick(question.Text_Ar, question.Text_En) ?? string.Empty,
                    HelpText = LocalizedText.Pick(question.Help_Text_Ar, question.Help_Text_En),
                    IsRequired = question.Is_Required,
                    SortOrder = question.Sort_Order,
                    MinValue = question.Min_Value,
                    MaxValue = question.Max_Value,
                    MinLabel = LocalizedText.Pick(question.Min_Label_Ar, question.Min_Label_En),
                    MaxLabel = LocalizedText.Pick(question.Max_Label_Ar, question.Max_Label_En),
                    Options =
                    [
                        .. options
                            .FindAll(option => option.Question_Id == question.Id)
                            .Select(option => new SurveyQuestionOptionDto
                            {
                                Id = option.Id,
                                Text = LocalizedText.Pick(option.Text_Ar, option.Text_En) ?? string.Empty,
                                SortOrder = option.Sort_Order,
                            }),
                    ],
                }),
            ],
        };
    }
}
