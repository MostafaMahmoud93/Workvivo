using Shouldly;
using Workvivo.Application.Features.Auth.Commands.Login;
using Xunit;

namespace Workvivo.Application.Tests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void A_complete_credential_passes()
    {
        _validator.Validate(new LoginCommand("dev", "P@55w0rd")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("user", "")]
    public void A_missing_field_fails(string userName, string password)
    {
        _validator.Validate(new LoginCommand(userName, password)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_oversized_field_fails_before_reaching_the_database()
    {
        // A megabyte-long username should be rejected by the validator rather than
        // becoming a parameter on a query.
        _validator.Validate(new LoginCommand(new string('a', 5000), "x")).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void The_validator_does_not_enforce_password_composition()
    {
        // Deliberate: validating the shape of a *submitted* password tells anyone
        // probing the endpoint what the policy is, and it is checked against a hash
        // regardless. Composition rules belong on password change, not on sign-in.
        _validator.Validate(new LoginCommand("dev", "a")).IsValid.ShouldBeTrue();
    }
}
