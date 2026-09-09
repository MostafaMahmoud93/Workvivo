using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Workvivo.API.Authorization;
using Workvivo.Domain.Abstractions.Interfaces;
using Xunit;

namespace Workvivo.API.Tests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static ClaimsPrincipal SignedIn(Guid userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "TestAuth"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private static async Task<AuthorizationHandlerContext> EvaluateAsync(
        IPermissionService permissions,
        ClaimsPrincipal user,
        string permission)
    {
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler(permissions, NullLogger<PermissionAuthorizationHandler>.Instance)
            .HandleAsync(context);

        return context;
    }

    [Fact]
    public async Task A_user_holding_the_permission_is_allowed()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.HasPermissionAsync(UserId, "Post.Create", Arg.Any<CancellationToken>())
            .Returns(true);

        var context = await EvaluateAsync(permissions, SignedIn(UserId), "Post.Create");

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task A_user_without_the_permission_is_refused()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.HasPermissionAsync(UserId, "Post.Delete", Arg.Any<CancellationToken>())
            .Returns(false);

        var context = await EvaluateAsync(permissions, SignedIn(UserId), "Post.Delete");

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task An_anonymous_caller_is_left_unmet_rather_than_failed()
    {
        var permissions = Substitute.For<IPermissionService>();

        var context = await EvaluateAsync(permissions, Anonymous(), "Post.Create");

        // Leaving the requirement unmet lets the framework issue a 401 challenge.
        // Calling Fail() would force a 403, which tells someone who has not signed in
        // that they are forbidden rather than that they should sign in.
        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeFalse();

        await permissions.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_principal_with_an_unusable_subject_claim_is_refused()
    {
        var permissions = Substitute.For<IPermissionService>();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")], "TestAuth"));

        var context = await EvaluateAsync(permissions, principal, "Post.Create");

        context.HasSucceeded.ShouldBeFalse();

        // Importantly it does not fall back to querying some default id.
        await permissions.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
