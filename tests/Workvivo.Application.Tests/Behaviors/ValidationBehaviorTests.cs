using FluentValidation;
using MediatR;
using Shouldly;
using Workvivo.Application.Behaviors;
using Workvivo.Application.Common.Messaging;
using Xunit;
using ValidationException = Workvivo.Domain.Exceptions.ValidationException;

namespace Workvivo.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record CreateThing(string Title, int Count) : ICommand<Guid>;

    private sealed class CreateThingValidator : AbstractValidator<CreateThing>
    {
        public CreateThingValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Count).GreaterThan(0).WithMessage("Count must be positive.");
        }
    }

    private static Task<Guid> Handler() => Task.FromResult(Guid.NewGuid());

    [Fact]
    public async Task Passes_a_valid_request_through_to_the_handler()
    {
        var behavior = new ValidationBehavior<CreateThing, Guid>([new CreateThingValidator()]);
        var expected = Guid.NewGuid();

        var result = await behavior.Handle(
            new CreateThing("Ok", 1),
            () => Task.FromResult(expected),
            CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task Collects_every_failure_rather_than_stopping_at_the_first()
    {
        // Returning one error at a time makes a user fix a form field by field. All
        // validators run, and all failures come back together.
        var behavior = new ValidationBehavior<CreateThing, Guid>([new CreateThingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(new CreateThing("", 0), Handler, CancellationToken.None));

        exception.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Property_names_are_camelCased_to_match_the_JSON_the_client_sent()
    {
        var behavior = new ValidationBehavior<CreateThing, Guid>([new CreateThingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(new CreateThing("", 5), Handler, CancellationToken.None));

        // "Title" would not match the Angular form control named "title", so the error
        // would render as a form-level message instead of against the field.
        exception.Errors.Keys.ShouldContain("title");
        exception.Errors.Keys.ShouldNotContain("Title");
    }

    [Fact]
    public async Task A_request_with_no_registered_validator_is_not_blocked()
    {
        var behavior = new ValidationBehavior<CreateThing, Guid>([]);
        var expected = Guid.NewGuid();

        var result = await behavior.Handle(
            new CreateThing("", -1),
            () => Task.FromResult(expected),
            CancellationToken.None);

        result.ShouldBe(expected);
    }
}
