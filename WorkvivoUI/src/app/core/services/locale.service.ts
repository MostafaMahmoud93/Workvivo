import { computed, Injectable, signal } from '@angular/core';

import { AR, EN, Messages } from '../i18n/messages';

export type AppLanguage = 'en' | 'ar';

const STORAGE_KEY = 'workvivo.language';

/**
 * Interface language and reading direction.
 *
 * Content direction is a separate matter - an Arabic reader may well be
 * looking at English content - so any view showing user content should
 * decide its own direction rather than following this one.
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly _language = signal<AppLanguage>(readStoredLanguage());

  readonly language = this._language.asReadonly();

  readonly direction = computed<'ltr' | 'rtl'>(() => (this._language() === 'ar' ? 'rtl' : 'ltr'));

  /** BCP 47 tag for `Intl` formatting. */
  readonly locale = computed(() => (this._language() === 'ar' ? 'ar' : 'en-GB'));

  readonly text = computed<Messages>(() => (this._language() === 'ar' ? AR : EN));

  constructor() {
    this.apply(this._language());
  }

  use(language: AppLanguage): void {
    this._language.set(language);
    this.apply(language);

    try {
      localStorage.setItem(STORAGE_KEY, language);
    } catch {
      // Blocked site data - the choice lasts for this tab only.
    }
  }

  toggle(): void {
    this.use(this._language() === 'ar' ? 'en' : 'ar');
  }

  private apply(language: AppLanguage): void {
    const root = document.documentElement;
    root.lang = language;
    root.dir = language === 'ar' ? 'rtl' : 'ltr';
  }
}

function readStoredLanguage(): AppLanguage {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'ar' || stored === 'en') {
      return stored;
    }
  } catch {
    // Fall through to the browser preference.
  }
  return navigator.language?.toLowerCase().startsWith('ar') ? 'ar' : 'en';
}
