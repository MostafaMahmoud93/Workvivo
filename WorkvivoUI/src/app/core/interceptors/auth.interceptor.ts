import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { AuthService } from '../services/auth.service';

/** Attaches the bearer token issued by api/Auth/Login. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).session()?.token;

  if (!token) {
    return next(request);
  }
  return next(
    request.clone({ setHeaders: { Authorization: 'Bearer ' + token } }),
  );
};
