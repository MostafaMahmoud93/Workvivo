import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, map, of, shareReplay, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginModel } from '../models/login-model';
import { ServiceResponse } from '../models/service-response';
import { AuthenticatedUser, SessionResponse } from '../models/session';

const api = (path: string) => environment.apiBaseUrl + '/api/auth/' + path;

/**
 * Holds the session.
 *
 * The access token lives in a signal and nowhere else - not localStorage, not
 * sessionStorage. Anything readable by script is readable by an XSS payload, and this
 * token is the one that opens the API. Losing it on reload is not a problem: the
 * HttpOnly refresh cookie survives, and `restore()` trades it for a new one during app
 * start-up. Long-lived credential out of reach of script; short-lived one in memory
 * only.
 *
 * The route paths are lowercase deliberately - the refresh cookie is scoped to
 * `/api/auth`, and cookie path matching is case-sensitive, so `/api/Auth/refresh`
 * would not carry it.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _accessToken = signal<string | null>(null);
  private readonly _user = signal<AuthenticatedUser | null>(null);
  private readonly _restored = signal(false);

  /** In-memory only. Read by the interceptor on each request. */
  readonly accessToken = this._accessToken.asReadonly();

  readonly user = this._user.asReadonly();

  /** True once the start-up refresh attempt has finished, whatever its outcome. */
  readonly restored = this._restored.asReadonly();

  readonly isAuthenticated = computed(() => this._accessToken() !== null);

  readonly permissions = computed(() => this._user()?.permissions ?? []);

  private restoring?: Observable<boolean>;

  login(model: LoginModel): Observable<ServiceResponse<SessionResponse>> {
    return this.http
      // withCredentials so the browser accepts the Set-Cookie carrying the refresh
      // token. Without it the sign-in appears to work and every later refresh fails.
      .post<ServiceResponse<SessionResponse>>(api('login'), model, { withCredentials: true })
      .pipe(
        tap((response) => {
          if (response.success && response.data) {
            this.apply(response.data);
          }
        }),
      );
  }

  /**
   * Exchanges the refresh cookie for a new access token.
   *
   * Shared, so several requests failing with 401 at once produce one refresh rather
   * than a burst of them - and a burst is not merely wasteful here: rotation means the
   * second request would present an already-used token and trip replay detection,
   * revoking the family and signing the user out.
   */
  refresh(): Observable<boolean> {
    this.restoring ??= this.http
      .post<ServiceResponse<SessionResponse>>(api('refresh'), {}, { withCredentials: true })
      .pipe(
        map((response) => {
          if (response.success && response.data) {
            this.apply(response.data);
            return true;
          }
          return false;
        }),
        catchError(() => {
          this.clear();
          return of(false);
        }),
        tap(() => {
          this.restoring = undefined;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.restoring;
  }

  /**
   * Called once during app start-up. Silently re-establishes the session if the
   * refresh cookie is still valid, so a reload does not look like a sign-out.
   */
  restore(): Observable<boolean> {
    return this.refresh().pipe(tap(() => this._restored.set(true)));
  }

  logout(allDevices = false): void {
    const path = allDevices ? 'logout-all' : 'logout';

    this.http.post(api(path), {}, { withCredentials: true }).subscribe({
      // Local state is cleared either way. A server that cannot be reached must not
      // leave someone looking signed in on a shared machine.
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  /** True when the signed-in user holds the named permission. */
  has(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasAny(...permissions: string[]): boolean {
    return permissions.some((permission) => this.has(permission));
  }

  private apply(session: SessionResponse): void {
    this._accessToken.set(session.accessToken);
    this._user.set(session.user);
  }

  private clear(): void {
    this._accessToken.set(null);
    this._user.set(null);
  }

  private finishLogout(): void {
    this.clear();
    void this.router.navigate(['/login']);
  }
}
