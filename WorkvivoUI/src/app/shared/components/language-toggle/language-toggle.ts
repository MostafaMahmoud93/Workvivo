import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { LocaleService } from '../../../core/services/locale.service';

/** Two-state switch between the English and Arabic interfaces. */
@Component({
  selector: 'app-language-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="lang" role="group" [attr.aria-label]="text().app.language">
      <button
        type="button"
        [class.is-on]="language() === 'en'"
        [attr.aria-pressed]="language() === 'en'"
        (click)="locale.use('en')"
      >
        EN
      </button>
      <button
        type="button"
        lang="ar"
        [class.is-on]="language() === 'ar'"
        [attr.aria-pressed]="language() === 'ar'"
        (click)="locale.use('ar')"
      >
        ع
      </button>
    </div>
  `,
  styleUrl: './language-toggle.scss',
})
export class LanguageToggle {
  protected readonly locale = inject(LocaleService);

  readonly language = this.locale.language;
  readonly text = this.locale.text;
}
