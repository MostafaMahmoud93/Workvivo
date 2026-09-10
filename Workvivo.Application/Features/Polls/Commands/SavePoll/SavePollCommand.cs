using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Polls;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Polls.Commands.SavePoll;

public sealed record PollOptionInput(string TextAr, string? TextEn);

/// <summary>Creates a poll and, unless it is a draft, opens it immediately.</summary>
public sealed record SavePollCommand(
    string QuestionAr,
    string? QuestionEn,
    IReadOnlyList<PollOptionInput> Options,
    bool IsMultipleChoice,
    bool IsAnonymous,
    bool ShowResultsBeforeVoting,
    DateTime? ExpiryDate,
    bool OpenNow,
    IReadOnlyList<Guid> AudienceDepartmentIds) : ICommand<Guid>;

public sealed class SavePollCommandValidator : AbstractValidator<SavePollCommand>
{
    public SavePollCommandValidator()
    {
        RuleFor(x => x.QuestionAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.QuestionEn).MaximumLength(500);

        // Two is the minimum that makes a question a choice; ten is where a poll stops
        // being a poll and should have been a survey.
        RuleFor(x => x.Options)
            .Must(options => options.Count is >= 2 and <= 10)
            .WithMessage("A poll needs between two and ten options.");

        RuleForEach(x => x.Options).ChildRules(option =>
            option.RuleFor(o => o.TextAr).NotEmpty().MaximumLength(200));

        RuleFor(x => x.ExpiryDate)
            .Must(date => date is null || date > DateTime.UtcNow)
            .WithMessage("A closing time must be in the future.");
    }
}

public sealed class SavePollCommandHandler : IRequestHandler<SavePollCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public SavePollCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(SavePollCommand request, CancellationToken cancellationToken)
    {
        await _currentEmployee.RequireIdAsync(cancellationToken);

        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.PollManage, cancellationToken))
        {
            throw new ForbiddenException("You cannot create polls.", PermissionKeys.PollManage);
        }

        var now = _clock.UtcNow;

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Question_Ar = _sanitizer.ToPlainText(request.QuestionAr),
            Question_En = request.QuestionEn is null ? null : _sanitizer.ToPlainText(request.QuestionEn),
            Is_Multiple_Choice = request.IsMultipleChoice,
            Is_Anonymous = request.IsAnonymous,
            Show_Results_Before_Voting = request.ShowResultsBeforeVoting,
            Status = request.OpenNow ? PollStatus.Open : PollStatus.Draft,
            Start_Date = request.OpenNow ? now : null,
            Expiry_Date = request.ExpiryDate,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Poll, Guid>().AddAsync(poll);

        var order = 0;

        foreach (var option in request.Options)
        {
            poll.Options.Add(new PollOption
            {
                Id = Guid.NewGuid(),
                Poll_Id = poll.Id,

                // Plain text. A poll option is rendered inside a label and a results
                // bar; there is no case for markup and every case against it.
                Text_Ar = _sanitizer.ToPlainText(option.TextAr),
                Text_En = option.TextEn is null ? null : _sanitizer.ToPlainText(option.TextEn),
                Sort_Order = order++,
                Is_Deleted = false,
            });
        }

        if (request.AudienceDepartmentIds.Count == 0)
        {
            poll.Audiences.Add(new PollAudience(poll.Id, AudienceType.AllEmployees, null));
        }
        else
        {
            foreach (var departmentId in request.AudienceDepartmentIds.Distinct())
            {
                poll.Audiences.Add(new PollAudience(poll.Id, AudienceType.Department, departmentId));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return poll.Id;
    }
}
