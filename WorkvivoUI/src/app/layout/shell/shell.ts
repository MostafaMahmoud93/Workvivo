import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { BrandMark } from '../../shared/components/brand-mark/brand-mark';
import { LanguageToggle } from '../../shared/components/language-toggle/language-toggle';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, BrandMark, LanguageToggle],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);

  readonly session = this.auth.session;
  readonly text = this.locale.text;

  signOut(): void {
    this.auth.logout();
  }
}
