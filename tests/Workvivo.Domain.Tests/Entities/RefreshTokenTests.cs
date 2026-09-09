using Shouldly;
using Workvivo.Domain.Entities.Identity;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void An_unexpired_unrevoked_token_is_active()
    {
        new RefreshToken { Expires_At = Now.AddDays(1) }.IsActiveAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void An_expired_token_is_not_active()
    {
        new RefreshToken { Expires_At = Now.AddSeconds(-1) }.IsActiveAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void A_token_expiring_exactly_now_is_not_active()
    {
        new RefreshToken { Expires_At = Now }.IsActiveAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void A_revoked_token_is_not_active_even_before_it_expires()
    {
        // This is the case replay detection depends on: presenting a revoked but
        // unexpired token is the signal that two parties hold the same credential.
        new RefreshToken { Expires_At = Now.AddDays(1), Revoked_At = Now.AddMinutes(-5) }
            .IsActiveAt(Now).ShouldBeFalse();
    }
}
