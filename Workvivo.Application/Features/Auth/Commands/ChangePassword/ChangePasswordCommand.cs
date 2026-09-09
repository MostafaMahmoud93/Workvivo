using FluentValidation;
using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Exceptions;
using ValidationException = Workvivo.Domain.Exceptions.ValidationException;

namespace Workvivo.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        // Length is the requirement that actually correlates with strength. Composition
        // rules mostly produce "Password1!" and are not enforced beyond a floor here;
        // Identity's own configured policy applies on top.
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Use at least 12 characters.")
            .MaximumLength(256)
            .NotEqual(x => x.CurrentPassword).WithMessage("Choose a password you have not used here before.");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokens,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var user = await _unitOfWork.UserManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedException();

        var result = await _unitOfWork.UserManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new ValidationException(
                "currentPassword",
                result.Errors.FirstOrDefault()?.Description ?? "The password could not be changed.");
        }

        // Changing a password is how somebody responds to a suspected compromise, so it
        // has to end the other sessions - otherwise whoever they are worried about keeps
        // their refresh token and the change achieves nothing.
        await _refreshTokens.RevokeAllForUserAsync(userId, "Password changed", cancellationToken);
    }
}
