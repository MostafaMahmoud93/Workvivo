using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Workvivo.Application.Behaviors;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Xunit;

namespace Workvivo.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record WriteThing : ICommand<int>;

    private sealed record ReadThing : IQuery<int>;

    /// <summary>
    /// A command whose failure path must persist something. Refresh-token replay
    /// detection is the real one: it revokes the compromised family and then rejects
    /// the request.
    /// </summary>
    private sealed record SelfManagedThing : ICommand<int>, INonTransactionalCommand;

    private static TransactionBehavior<TRequest, int> Create<TRequest>(IUnitOfWork unitOfWork)
        where TRequest : notnull =>
        new(unitOfWork, NullLogger<TransactionBehavior<TRequest, int>>.Instance);

    [Fact]
    public async Task A_command_is_wrapped_in_a_transaction()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();

        await Create<WriteThing>(unitOfWork)
            .Handle(new WriteThing(), () => Task.FromResult(1), CancellationToken.None);

        await unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failing_command_is_rolled_back()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            Create<WriteThing>(unitOfWork).Handle(
                new WriteThing(),
                () => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        await unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_query_is_not_wrapped()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();

        await Create<ReadThing>(unitOfWork)
            .Handle(new ReadThing(), () => Task.FromResult(1), CancellationToken.None);

        await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_self_managing_command_is_not_wrapped_even_though_it_is_a_command()
    {
        // This pins a real bug. Refresh-token replay detection revokes the whole token
        // family and then throws to reject the request. Inside the ambient transaction
        // the throw rolled that revocation back, so the replay was refused while the
        // compromised session stayed alive - defeating the point of detecting it.
        var unitOfWork = Substitute.For<IUnitOfWork>();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            Create<SelfManagedThing>(unitOfWork).Handle(
                new SelfManagedThing(),
                () => throw new InvalidOperationException("rejected after persisting a revocation"),
                CancellationToken.None));

        await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_already_open_transaction_is_not_nested()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasActiveTransaction.Returns(true);

        await Create<WriteThing>(unitOfWork)
            .Handle(new WriteThing(), () => Task.FromResult(1), CancellationToken.None);

        await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }
}
