import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

/** Endpoints that must never trigger a refresh attempt - refreshing is what they do. */
const authPaths = ['/api/auth/login', '/api/auth/refresh', '/api/auth/logout'];

/**
 * Attaches the access token, and recovers from an expired one.
 *
 * Access tokens last fifteen minutes, so expiry mid-session is normal rather than
 * exceptional. On a 401 this refreshes once and replays the original request, which is
 * what keeps a short token lifetime from being felt by the user.
 *
 * The refresh itself is shared by AuthService, so ten requests expiring together
 * produce one refresh. That matters beyond efficiency: with rotation, a second
 * concurrent refresh would present an already-used token, trip replay detection, and
 * revoke the whole family.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  const isAuthEndpoint = authPaths.some((path) => request.url.includes(path));
  const authorized = attachToken(request, auth.accessToken());

  return next(authorized).pipe(
    catchError((error: unknown) => {
      const shouldRetry =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAuthEndpoint &&
        auth.accessToken() !== null;

      if (!shouldRetry) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((refreshed) => {
          if (!refreshed) {
            return throwError(() => error);
          }
          // Replayed with the new token. The body of the original request is reused
          // as-is, so the retry is invisible to the caller.
          return next(attachToken(request, auth.accessToken()));
        }),
      );
    }),
  );
};

function attachToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) {
    return request;
  }

  return request.clone({ setHeaders: { Authorization: 'Bearer ' + token } });
}
