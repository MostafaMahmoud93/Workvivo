using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// Wraps a command in a single database transaction so a handler that writes several
/// tables - a post, its attachments, its audience rows - either lands completely or
/// not at all.
///
/// Queries are excluded: a read transaction buys nothing and holds locks.
/// Side effects that leave the database (email, push, real-time) must be raised as
/// domain events dispatched after commit, never inside this scope - otherwise a
/// rollback still sends the email.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // A handler that manages its own persistence opts out. See
        // INonTransactionalCommand for why that is ever the right thing to do.
        if (request is INonTransactionalCommand)
        {
            return await next();
        }

        var isCommand = request is ICommand || IsGenericCommand(typeof(TRequest));
        if (!isCommand)
        {
            return await next();
        }

        // A transaction is only opened if the handler has not already started one -
        // nested BeginTransaction on the same connection throws.
        if (_unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogDebug("Rolled back the transaction for {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }

    private static bool IsGenericCommand(Type requestType) =>
        Array.Exists(
            requestType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
