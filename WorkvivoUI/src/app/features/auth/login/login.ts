import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { BrandMark } from '../../../shared/components/brand-mark/brand-mark';
import { LanguageToggle } from '../../../shared/components/language-toggle/language-toggle';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, BrandMark, LanguageToggle],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  readonly form = inject(FormBuilder).nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required],
  });

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.busy.set(false);
        if (response.success && response.data?.token) {
          void this.router.navigateByUrl(this.returnUrl());
          return;
        }
        // HTTP 200 + success:false is how the API reports a rejected login.
        this.error.set(response.message || this.text().login.failed);
      },
      error: () => {
        this.busy.set(false);
        this.error.set(this.text().login.unreachable);
      },
    });
  }

  private returnUrl(): string {
    const requested = this.router.parseUrl(this.router.url).queryParams['returnUrl'];
    return typeof requested === 'string' && requested.startsWith('/') ? requested : '/home';
  }
}
