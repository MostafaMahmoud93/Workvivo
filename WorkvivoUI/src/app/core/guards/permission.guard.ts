import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/**
 * Keeps a route out of reach of someone without the permission it needs.
 *
 *   { path: 'admin/audit', canActivate: [authGuard, permissionGuard('AuditLog.View')] }
 *
 * Convenience, not security. The client holds a list the server sent it and could be
 * made to say anything; the endpoint behind the route checks again. What this actually
 * buys is a user not being shown a screen that would only fill with 403s.
 */
export const permissionGuard = (...permissions: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.hasAny(...permissions)) {
      return true;
    }

    return router.createUrlTree(['/home']);
  };
};
