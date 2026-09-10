import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { LocaleService } from '../services/locale.service';

/**
 * Tells the API which language to answer in.
 *
 * Most copy in this product is translated in the client, so for a long time nothing
 * needed this. Notifications changed that: their text is written by the server, in
 * both languages, and stored - so the server has to be told which side to return. It
 * reads Accept-Language, and the browser's own header reflects the operating system,
 * not the toggle in the header bar. Without this an employee who switches the
 * interface to Arabic keeps getting English notifications and cannot work out why.
 *
 * The full culture, not just "ar": the API's supported cultures are ar-AE and en-US,
 * and although it falls back to parent cultures, sending what it actually supports
 * avoids depending on that.
 */
export const localeInterceptor: HttpInterceptorFn = (request, next) => {
  const locale = inject(LocaleService);

  return next(
    request.clone({
      setHeaders: {
        'Accept-Language': locale.language() === 'ar' ? 'ar-AE' : 'en-US',
      },
    }),
  );
};
