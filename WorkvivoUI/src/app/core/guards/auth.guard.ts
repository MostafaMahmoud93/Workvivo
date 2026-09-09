import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from '../services/auth.service';

/**
 * Keeps signed-out visitors out of the shell.
 *
 * On a cold start the access token is always absent - it lives only in memory - so the
 * guard waits for the start-up refresh before deciding. Without that wait, every
 * reload of a protected page would bounce to the login screen and then immediately
 * back again.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  if (auth.restored()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  return auth.restore().pipe(
    map((restored) =>
      restored ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }),
    ),
  );
};

/** Keeps signed-in users off the login screen. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/home']);
  }

  if (auth.restored()) {
    return true;
  }

  return auth.restore().pipe(map((restored) => (restored ? router.createUrlTree(['/home']) : true)));
};
