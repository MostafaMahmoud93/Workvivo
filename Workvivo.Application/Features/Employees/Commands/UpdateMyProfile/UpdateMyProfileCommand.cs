using FluentValidation;
using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Employees.Commands.UpdateMyProfile;

/// <summary>
/// Edits the caller's own profile.
///
/// There is no employee id on this command, and that is the security design rather than
/// an omission: the record edited is resolved from the authenticated principal, so
/// there is no parameter to tamper with and no way to reach somebody else's profile.
/// An "edit employee X" endpoint is a separate, permission-guarded command.
///
/// The fields are also deliberately limited to the ones people own about themselves.
/// Department, job title, employee number and manager are HR's to set - accepting them
/// here would let anyone promote themselves.
/// </summary>
public sealed record UpdateMyProfileCommand(
    string DisplayName,
    string? Mobile,
    string? Extension,
    string? BiographyAr,
    string? BiographyEn,
    string PreferredLanguage,
    bool ShowBirthday) : ICommand;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private static readonly string[] SupportedLanguages = ["en", "ar"];

    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Mobile).MaximumLength(32);
        RuleFor(x => x.Extension).MaximumLength(32);
        RuleFor(x => x.BiographyAr).MaximumLength(2000);
        RuleFor(x => x.BiographyEn).MaximumLength(2000);

        RuleFor(x => x.PreferredLanguage)
            .Must(language => SupportedLanguages.Contains(language))
            .WithMessage("Choose either English or Arabic.");
    }
}

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IContentSanitizer _sanitizer;

    public UpdateMyProfileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IContentSanitizer sanitizer)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
    }

    public async Task Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var employee = await _unitOfWork.Repository<Employee, Guid>()
            .FirstOrDefaultAsync(candidate => candidate.User_Id == userId)
            ?? throw new NotFoundException("You do not have an employee profile.");

        employee.Display_Name = request.DisplayName.Trim();
        employee.Mobile = request.Mobile?.Trim();
        employee.Extension = request.Extension?.Trim();

        // A biography is shown on a page other people load, so it is user-authored
        // content on a rendered surface - the same threat as a post body. Stripped to
        // plain text on the way in, because a profile has no need for markup at all.
        employee.Biography_Ar = _sanitizer.ToPlainText(request.BiographyAr);
        employee.Biography_En = _sanitizer.ToPlainText(request.BiographyEn);

        employee.Preferred_Language = request.PreferredLanguage;
        employee.Show_Birthday = request.ShowBirthday;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
