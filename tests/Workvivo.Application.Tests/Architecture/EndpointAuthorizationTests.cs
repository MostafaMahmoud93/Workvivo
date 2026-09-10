using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Xunit;

namespace Workvivo.Application.Tests.Architecture;

/// <summary>
/// Every command and query has to decide who may run it, and the decision has to be
/// somewhere a reader can find.
///
/// This is a guard against the specific mistake that has already happened once in
/// this codebase: an endpoint shipped with its permission filter commented out, and
/// nothing failed. A test that reads the handlers cannot prove a rule is correct -
/// only that one was written - but that is the difference between a gap somebody
/// chose and a gap nobody noticed.
/// </summary>
public class EndpointAuthorizationTests
{
    private static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    /// <summary>
    /// Handlers whose authorisation lives somewhere this test cannot see - at the
    /// controller, or inside the query itself - together with the reason.
    ///
    /// The reason is the point. An entry here is somebody saying "I looked at this
    /// and it is fine", which is a different thing from a handler nobody examined.
    /// The companion test below fails if an entry outlives its handler, so the list
    /// cannot quietly rot into a blanket exemption.
    /// </summary>
    private static readonly Dictionary<string, string> DeliberatelyOpen = new()
    {
        // Gated at the controller by a [HasPermission] attribute.
        ["SaveDepartmentCommandHandler"] = "controller requires Organization.Manage",
        ["DeleteDepartmentCommandHandler"] = "controller requires Organization.Manage",
        ["GetDepartmentTreeQueryHandler"] = "controller requires Employee.View",
        ["GetEmployeeDirectoryQueryHandler"] = "controller requires Employee.View",
        ["GetEmployeeProfileQueryHandler"] = "controller requires Employee.View",
        ["SearchEmployeesQueryHandler"] = "controller requires Employee.View",
        ["GetDocumentCategoriesQueryHandler"] = "controller requires Document.View; returns names and counts only",

        // The decision is made inside the query rather than by an injected service.
        ["GetCommunitiesQueryHandler"] = "hides Private communities from non-members in the query itself",
        ["GetRecognitionWallQueryHandler"] = "applies the three visibility levels in the query itself",

        // Genuinely open to every authenticated employee.
        ["GetLeaderboardQueryHandler"] = "a leaderboard is public by definition",

        ["GetMyNotificationsQueryHandler"] = "reads only the caller's own notifications",
        ["GetUnreadCountQueryHandler"] = "counts only the caller's own notifications",
        ["MarkNotificationsReadCommandHandler"] = "writes only the caller's own rows",
        ["GetNotificationPreferencesQueryHandler"] = "reads the caller's own preferences",
        ["UpdateNotificationPreferencesCommandHandler"] = "writes the caller's own preferences",
        ["GetCurrentSessionQueryHandler"] = "describes the caller's own session",
        ["LoginCommandHandler"] = "is the authentication entry point",
        ["RefreshTokenCommandHandler"] = "is the authentication entry point",
        ["LogoutCommandHandler"] = "ends the caller's own session",
        ["ChangePasswordCommandHandler"] = "changes the caller's own password",
        ["UpdateMyProfileCommandHandler"] = "writes the caller's own profile",
        ["FollowEmployeeCommandHandler"] = "records the caller's own follow",
        ["GetOrganizationLookupsQueryHandler"] = "returns reference data every screen needs",
        ["GetRecognitionTypesQueryHandler"] = "returns reference data for the recognition form",
        ["GiveRecognitionCommandHandler"] = "recognition is open to everybody by design",
        ["GetMyInvitationsQueryHandler"] = "reads the caller's own invitations",
        ["RespondToInvitationCommandHandler"] = "answers an invitation addressed to the caller",
        ["SearchQueryHandler"] = "filters every branch by the caller's audience keys",
    };

    /// <summary>
    /// Names that count as making an authorisation decision - a permission check, an
    /// audience filter, or a resource-level authorisation helper.
    /// </summary>
    private static readonly string[] AuthorisationSignals =
    [
        "IPermissionService",
        "IAudienceResolver",
        "PostAuthorization",
        "CommunityAuthorization",
    ];

    [Fact]
    public void Every_handler_either_checks_something_or_is_listed_as_deliberately_open()
    {
        var handlers = ApplicationAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => Array.Exists(
                type.GetInterfaces(),
                contract => contract.IsGenericType
                    && contract.GetGenericTypeDefinition().Name.StartsWith("IRequestHandler", StringComparison.Ordinal)))
            .ToList();

        handlers.ShouldNotBeEmpty("the reflection query itself has to keep working");

        var unaccounted = handlers
            .Where(handler => !DeliberatelyOpen.ContainsKey(handler.Name))
            .Where(handler => !ChecksSomething(handler))
            .Select(handler => handler.Name)
            .OrderBy(name => name)
            .ToList();

        unaccounted.ShouldBeEmpty(
            "these handlers make no authorisation decision and are not listed as deliberately open: "
            + string.Join(", ", unaccounted));
    }

    [Fact]
    public void The_deliberately_open_list_does_not_name_handlers_that_no_longer_exist()
    {
        // A stale exemption is worse than none: it silently excuses a future handler
        // that happens to be given the same name.
        var names = ApplicationAssembly.GetTypes().Select(type => type.Name).ToHashSet();

        var stale = DeliberatelyOpen.Keys.Where(name => !names.Contains(name)).OrderBy(name => name).ToList();

        stale.ShouldBeEmpty("these exemptions name handlers that no longer exist: " + string.Join(", ", stale));
    }

    private static bool ChecksSomething(Type handler)
    {
        var constructor = handler.GetConstructors().FirstOrDefault();

        if (constructor is null)
        {
            return false;
        }

        return constructor.GetParameters()
            .Any(parameter => Array.Exists(
                AuthorisationSignals,
                signal => parameter.ParameterType.Name.Contains(signal, StringComparison.Ordinal)));
    }
}
