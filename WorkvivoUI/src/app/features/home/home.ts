import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);

  readonly session = this.auth.session;
  readonly isAdmin = this.auth.isAdmin;
  readonly text = this.locale.text;

  readonly actionCount = computed(() => this.session()?.userActions?.length ?? 0);

  readonly expiresAtLabel = computed(() => {
    const expiration = this.session()?.expiration;
    if (!expiration) {
      return '-';
    }
    return new Date(expiration).toLocaleString(this.locale.locale(), { dateStyle: 'medium', timeStyle: 'short' });
  });
}
