using Microsoft.AspNetCore.Http.Features;

namespace Workvivo.API.Tests.Support;

/// <summary>
/// An <see cref="IHttpResponseFeature"/> that runs its OnStarting callbacks when
/// <see cref="FireOnStartingAsync"/> is called.
///
/// The default in-memory feature behind DefaultHttpContext collects the callbacks and
/// drops them, because in a real request it is the server that invokes them. Any
/// middleware that defers work to OnStarting - which is the only correct way to set a
/// response header from middleware - is therefore untestable without this.
/// </summary>
internal sealed class StartableResponseFeature : HttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

    public override bool HasStarted { get; }

    public override void OnStarting(Func<object, Task> callback, object state) =>
        _callbacks.Add((callback, state));

    /// <summary>Invokes the callbacks in registration order, as Kestrel does.</summary>
    public async Task FireOnStartingAsync()
    {
        foreach (var (callback, state) in _callbacks)
        {
            await callback(state);
        }
    }
}
