using Microsoft.AspNetCore.Http;
using Shouldly;
using Workvivo.Domain.Exceptions;
using Xunit;

namespace Workvivo.Domain.Tests.Exceptions;

/// <summary>
/// The exception hierarchy is what the API's error middleware reads to decide a status
/// code, so these mappings are a contract, not an implementation detail. If one of them
/// drifts, clients start seeing the wrong status for a failure they branch on.
/// </summary>
public class AppExceptionTests
{
    [Fact]
    public void NotFound_maps_to_404()
    {
        var exception = new NotFoundException("Post", Guid.Empty);

        exception.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        exception.ErrorType.ShouldBe("not-found");
        exception.Message.ShouldContain("Post");
    }

    [Fact]
    public void Forbidden_does_not_expose_the_required_permission_to_the_client()
    {
        var exception = new ForbiddenException("Not allowed.", "Post.Moderate");

        // The permission name is available to the server for logging, but must not
        // reach the response body - it would hand an attacker a map of the model.
        exception.RequiredPermission.ShouldBe("Post.Moderate");
        exception.Extensions.ShouldBeNull();
        exception.Message.ShouldNotContain("Post.Moderate");
    }

    [Fact]
    public void BusinessRule_maps_to_422_and_surfaces_its_rule_code()
    {
        var exception = new BusinessRuleException("Replies cannot be nested this deep.", "comment.max-depth");

        exception.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        exception.Extensions.ShouldNotBeNull();
        exception.Extensions!["ruleCode"].ShouldBe("comment.max-depth");
    }

    [Fact]
    public void Validation_groups_messages_by_property()
    {
        var exception = new ValidationException(new Dictionary<string, string[]>
        {
            ["content"] = ["Content is required."],
            ["audiences"] = ["Select at least one audience."],
        });

        exception.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        exception.Errors.Count.ShouldBe(2);
        exception.Errors["content"].ShouldHaveSingleItem();
    }

    [Fact]
    public void RateLimit_reports_retry_after_in_whole_seconds()
    {
        var exception = new RateLimitException("Slow down.", TimeSpan.FromSeconds(90));

        exception.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        exception.Extensions!["retryAfterSeconds"].ShouldBe(90);
    }

    [Fact]
    public void Every_deliberate_failure_derives_from_AppException()
    {
        // The error middleware treats anything that is not an AppException as an
        // unexpected 500 with no detail. A new exception type that forgets to derive
        // from it would silently become an opaque server error.
        var exceptionTypes = typeof(AppException).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(AppException).Namespace && !t.IsAbstract);

        foreach (var type in exceptionTypes)
        {
            typeof(AppException).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} lives in the exceptions namespace but does not derive from AppException.");
        }
    }
}
