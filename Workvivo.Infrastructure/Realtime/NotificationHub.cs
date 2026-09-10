using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Workvivo.Infrastructure.Realtime;

/// <summary>
/// The live connection every signed-in client holds open.
///
/// Deliberately one-way. The hub exposes no methods a client can call to send anything
/// to anybody - a hub method is an unauthenticated-looking API endpoint that people
/// forget to secure, and this product has no need for one. The server pushes; the
/// client listens.
///
/// <c>[Authorize]</c> is not decoration. Without it the hub accepts anonymous
/// connections and <c>Context.UserIdentifier</c> is null, at which point every
/// user-targeted send silently goes nowhere.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    /// <summary>Where the hub is mounted. Must match the path the JWT handler accepts a query token for.</summary>
    public const string Path = "/hubs/notifications";

    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug(
            "Hub connection {ConnectionId} opened for user {UserId}",
            Context.ConnectionId,
            Context.UserIdentifier);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            // Logged at Debug, not Warning. Connections drop constantly - a laptop
            // sleeps, a tunnel closes, a phone changes network - and treating that as
            // an error buries the real ones.
            _logger.LogDebug(
                exception,
                "Hub connection {ConnectionId} closed with an error",
                Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Tells SignalR which claim identifies a user.
///
/// The default is <c>ClaimTypes.NameIdentifier</c>, which is what this product puts the
/// user id in - but the default is implicit, and if the token layout ever changes the
/// failure is silent: sends to a user simply stop arriving, with no error anywhere.
/// Stating it makes that dependency visible and testable.
/// </summary>
public sealed class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
