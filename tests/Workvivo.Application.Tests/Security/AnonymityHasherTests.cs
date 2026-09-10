using Microsoft.Extensions.Configuration;
using Shouldly;
using Workvivo.Infrastructure.Common;
using Xunit;

namespace Workvivo.Application.Tests.Security;

/// <summary>
/// Anonymous polls and surveys promise two things at once that pull against each
/// other: one response per person, and no record of who responded. The keyed hash is
/// what makes both true, so its properties are worth pinning individually - each one
/// failing breaks a different half of the promise.
/// </summary>
public class AnonymityHasherTests
{
    private const string Key = "a-test-key-that-is-at-least-32-characters-long";

    private static AnonymityHasher Create(string? key = Key) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [AnonymityHasher.KeyPath] = key })
            .Build());

    [Fact]
    public void The_same_person_in_the_same_poll_hashes_the_same_way()
    {
        // Without this, one person could vote as many times as they liked.
        var hasher = Create();
        var scope = Guid.NewGuid();
        var employee = Guid.NewGuid();

        hasher.Hash(scope, employee).ShouldBe(hasher.Hash(scope, employee));
    }

    [Fact]
    public void Two_people_in_the_same_poll_hash_differently()
    {
        var hasher = Create();
        var scope = Guid.NewGuid();

        hasher.Hash(scope, Guid.NewGuid()).ShouldNotBe(hasher.Hash(scope, Guid.NewGuid()));
    }

    [Fact]
    public void The_same_person_hashes_differently_in_two_polls()
    {
        // The property that stops the hashes being a stable pseudonym. Without the
        // scope in the message, one leaked mapping would deanonymise that person
        // across every anonymous instrument they ever answered.
        var hasher = Create();
        var employee = Guid.NewGuid();

        hasher.Hash(Guid.NewGuid(), employee).ShouldNotBe(hasher.Hash(Guid.NewGuid(), employee));
    }

    [Fact]
    public void A_different_key_produces_a_different_hash()
    {
        // What makes the hash irreversible in practice. Without a secret key an
        // attacker holding the table hashes every employee id and reads off the
        // voters in seconds.
        var scope = Guid.NewGuid();
        var employee = Guid.NewGuid();

        var first = Create().Hash(scope, employee);
        var second = Create("a-completely-different-key-of-at-least-32-chars").Hash(scope, employee);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void The_hash_does_not_contain_the_employee_id()
    {
        var scope = Guid.NewGuid();
        var employee = Guid.NewGuid();

        var hash = Create().Hash(scope, employee);

        hash.ShouldNotContain(employee.ToString("N"), Case.Insensitive);
        hash.ShouldNotContain(employee.ToString("D"), Case.Insensitive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void A_missing_or_weak_key_stops_the_application_starting(string? key)
    {
        // Refusing to start beats starting with a guessable key. With a weak one,
        // every anonymous response is reversible and nothing about the running
        // system looks wrong.
        Should.Throw<InvalidOperationException>(() => Create(key));
    }
}
