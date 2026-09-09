using FluentValidation;

namespace Workvivo.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Length bounds only. Validating the *shape* of a submitted password would
        // leak the policy to anyone probing the endpoint, and the password is checked
        // against a hash regardless.
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
