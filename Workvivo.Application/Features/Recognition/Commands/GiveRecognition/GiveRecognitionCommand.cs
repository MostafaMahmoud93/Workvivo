using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.Recognition;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Recognition.Commands.GiveRecognition;

/// <summary>Recognises a colleague.</summary>
public sealed record GiveRecognitionCommand(
    Guid RecipientEmployeeId,
    Guid RecognitionTypeId,
    string Message,
    RecognitionVisibility Visibility) : ICommand<Guid>;

public sealed class GiveRecognitionCommandValidator : AbstractValidator<GiveRecognitionCommand>
{
    public GiveRecognitionCommandValidator()
    {
        RuleFor(x => x.RecipientEmployeeId).NotEmpty();
        RuleFor(x => x.RecognitionTypeId).NotEmpty();

        // A minimum as well as a maximum. "Well done" with no substance is what a
        // recognition programme degenerates into when nothing asks for more.
        RuleFor(x => x.Message)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Say a little about why - at least ten characters.")
            .MaximumLength(2000);

        RuleFor(x => x.Visibility).Must(Enum.IsDefined).WithMessage("Unknown visibility.");
    }
}

public sealed class GiveRecognitionCommandHandler : IRequestHandler<GiveRecognitionCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public GiveRecognitionCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(GiveRecognitionCommand request, CancellationToken cancellationToken)
    {
        var senderId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        if (senderId == request.RecipientEmployeeId)
        {
            // The domain says the same thing, and so does a check constraint. Points
            // feed a leaderboard, so the rule is worth stating everywhere it can be
            // broken.
            throw new BusinessRuleException(
                "You cannot recognise yourself.", "recognition.self");
        }

        var recipient = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == request.RecipientEmployeeId && employee.Is_Active)
            .Select(employee => new { employee.Id, employee.Display_Name, employee.Department_Id })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.RecipientEmployeeId);

        var type = await _unitOfWork.Repository<RecognitionType, Guid>()
            .GetAllQ()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.RecognitionTypeId && candidate.Is_Active,
                cancellationToken)
            ?? throw new NotFoundException(nameof(RecognitionType), request.RecognitionTypeId);

        var sender = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == senderId)
            .Select(employee => new { employee.Display_Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("You do not have an employee profile.");

        // Plain text, sanitised. The message is written in a textarea and rendered in
        // a feed post, an email and a profile - the safe form is the one that never
        // carried markup in the first place.
        var message = _sanitizer.ToPlainText(request.Message);

        var recognition = new Domain.Entities.Recognition.Recognition
        {
            Id = Guid.NewGuid(),
            Sender_Employee_Id = senderId,
            Recipient_Employee_Id = request.RecipientEmployeeId,
            Recognition_Type_Id = type.Id,
            Message = message,

            // Points come from the category, not the request. A sender who could name
            // their own number would be deciding the leaderboard.
            Points = type.Default_Points,
            Visibility = request.Visibility,
            Recognised_On = now,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Domain.Entities.Recognition.Recognition, Guid>()
            .AddAsync(recognition);

        if (request.Visibility != RecognitionVisibility.Private)
        {
            recognition.Post_Id = await CreateFeedPostAsync(
                recognition, sender.Display_Name, recipient.Display_Name, type,
                recipient.Department_Id, senderId, now, cancellationToken);
        }

        // In the database, atomically. Two people recognising the same colleague at
        // once would otherwise both read the same total and write the same value back.
        await _unitOfWork.Repository<Employee, Guid>().ExecuteUpdateAsync(
            employee => employee.Id == request.RecipientEmployeeId,
            setters => setters.SetProperty(
                employee => employee.Recognition_Points,
                employee => employee.Recognition_Points + type.Default_Points),
            cancellationToken);

        recognition.RecordGiven(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return recognition.Id;
    }

    /// <summary>
    /// Puts a public recognition on the feed.
    ///
    /// A real post rather than a special feed row, so it gets reactions, comments and
    /// audience targeting for free - and so the feed query does not have to learn
    /// about a second kind of content.
    /// </summary>
    private async Task<Guid> CreateFeedPostAsync(
        Domain.Entities.Recognition.Recognition recognition,
        string senderName,
        string recipientName,
        RecognitionType type,
        Guid? recipientDepartmentId,
        Guid senderId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var typeName = type.Name_En ?? type.Name_Ar;

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Author_Employee_Id = senderId,
            Post_Type = PostType.Recognition,
            Title_En = $"{senderName} recognised {recipientName}",
            Title_Ar = $"{senderName} كرّم {recipientName}",
            Content_Html = $"<p><strong>{System.Net.WebUtility.HtmlEncode(typeName)}</strong></p>"
                + $"<p>{System.Net.WebUtility.HtmlEncode(recognition.Message)}</p>",
            Content_Text = $"{typeName}. {recognition.Message}",
            Comments_Enabled = true,
            Visibility = PostVisibility.Organization,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Post, Guid>().AddAsync(post);

        // Department-visible recognition is targeted at the recipient's department,
        // falling back to everyone when they have none - a recognition that reaches
        // nobody would be worse than one that reaches too many.
        if (recognition.Visibility == RecognitionVisibility.Department && recipientDepartmentId is { } departmentId)
        {
            post.Audiences.Add(new PostAudience(post.Id, AudienceType.Department, departmentId));
        }
        else
        {
            post.Audiences.Add(new PostAudience(post.Id, AudienceType.AllEmployees, null));
        }

        // The same domain method the composer and the scheduler use, so a recognition
        // post is published on identical terms - and so its audience is notified by
        // exactly the same path.
        post.Publish(now, []);

        await Task.CompletedTask;

        return post.Id;
    }
}
