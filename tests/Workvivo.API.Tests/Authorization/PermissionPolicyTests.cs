using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Shouldly;
using Workvivo.API.Authorization;
using Workvivo.Infrastructure.Seeding;
using Xunit;

namespace Workvivo.API.Tests.Authorization;

public class PermissionPolicyTests
{
    private static PermissionPolicyProvider CreateProvider() =>
        new(Microsoft.Extensions.Options.Options.Create(new AuthorizationOptions()));

    [Fact]
    public void The_attribute_encodes_the_permission_in_its_policy_name()
    {
        var attribute = new HasPermissionAttribute(Permissions.Post.Create);

        attribute.Policy.ShouldBe("Permission:Post.Create");
        attribute.Permission.ShouldBe(Permissions.Post.Create);
    }

    [Fact]
    public void A_permission_policy_name_round_trips()
    {
        var policy = new HasPermissionAttribute(Permissions.AuditLog.View).Policy!;

        HasPermissionAttribute.ExtractPermission(policy).ShouldBe(Permissions.AuditLog.View);
    }

    [Theory]
    [InlineData("SomeOtherPolicy")]
    [InlineData("")]
    [InlineData("permission")]
    public void A_name_that_is_not_a_permission_policy_is_not_treated_as_one(string policyName)
    {
        HasPermissionAttribute.ExtractPermission(policyName).ShouldBeNull();
    }

    [Fact]
    public async Task The_provider_manufactures_a_policy_for_any_permission()
    {
        // Permissions are rows in a table, so the set is not known at start-up and the
        // policy has to be built on demand. If this returned null the endpoint would
        // throw at request time rather than deny.
        var policy = await CreateProvider().GetPolicyAsync("Permission:Something.Invented");

        policy.ShouldNotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().ShouldHaveSingleItem()
            .Permission.ShouldBe("Something.Invented");
    }

    [Fact]
    public async Task A_manufactured_policy_also_requires_authentication()
    {
        var policy = await CreateProvider().GetPolicyAsync("Permission:Post.Create");

        policy!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Non_permission_policies_fall_through_to_the_default_provider()
    {
        // A plain [Authorize(Policy = "...")] elsewhere in the app must keep working.
        (await CreateProvider().GetPolicyAsync("SomethingElse")).ShouldBeNull();
    }
}
