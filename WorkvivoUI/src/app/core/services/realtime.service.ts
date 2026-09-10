import { DestroyRef, Injectable, effect, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/**
 * The live connection to the API.
 *
 * One connection for the whole application, opened when a session exists and closed
 * when it does not. Components subscribe to events through `on`, they do not each
 * build a connection - a hub connection is a socket, and one per component is one
 * socket per component.
 *
 * The access token is supplied through `accessTokenFactory` rather than baked into the
 * URL once. SignalR calls it again on every reconnect, so a connection that drops
 * after the token has been refreshed comes back with the new one instead of failing
 * silently until the page is reloaded.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private connection?: HubConnection;

  /** Handlers registered before the connection exists, replayed once it does. */
  private readonly handlers = new Map<string, ((payload: unknown) => void)[]>();

  private readonly _connected = signal(false);

  readonly connected = this._connected.asReadonly();

  constructor() {
    // Follows the session. Signing in opens the connection; signing out closes it,
    // which matters because a socket authenticated as the previous user would keep
    // pushing their notifications to the next one on a shared machine.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        void this.start();
      } else {
        void this.stop();
      }
    });

    this.destroyRef.onDestroy(() => void this.stop());
  }

  /** Registers a handler. Safe to call before the connection is up. */
  on<T>(eventName: string, handler: (payload: T) => void): void {
    const wrapped = handler as (payload: unknown) => void;

    const existing = this.handlers.get(eventName) ?? [];
    this.handlers.set(eventName, [...existing, wrapped]);

    this.connection?.on(eventName, wrapped);
  }

  private async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    // The whole thing is guarded, not just the handshake.
    //
    // `build()` throws synchronously when the environment cannot support a transport -
    // which is true in a jsdom test run, and true in a browser behind a proxy that
    // blocks WebSockets. Catching only around `start()` let that throw escape an
    // effect, where it surfaces as an unhandled error and takes unrelated code with
    // it. Real-time is an enhancement: every notification is also a row the client can
    // fetch, so nothing here may break the page.
    try {
      const connection = new HubConnectionBuilder()
        .withUrl(environment.apiBaseUrl + '/hubs/notifications', {
          accessTokenFactory: () => this.auth.accessToken() ?? '',
        })
        // Default backoff, then keep trying every 30s. The default policy gives up
        // after about a minute, which on a laptop that was asleep means the bell stops
        // working until the page is reloaded.
        .withAutomaticReconnect([0, 2000, 10000, 30000, 30000, 30000])
        .configureLogging(LogLevel.Warning)
        .build();

      for (const [eventName, handlers] of this.handlers) {
        for (const handler of handlers) {
          connection.on(eventName, handler);
        }
      }

      connection.onreconnected(() => this._connected.set(true));
      connection.onclose(() => this._connected.set(false));

      this.connection = connection;

      await connection.start();
      this._connected.set(true);
    } catch {
      this._connected.set(false);
    }
  }

  private async stop(): Promise<void> {
    const connection = this.connection;

    if (!connection) {
      return;
    }

    this.connection = undefined;
    this._connected.set(false);

    if (connection.state !== HubConnectionState.Disconnected) {
      await connection.stop();
    }
  }
}
