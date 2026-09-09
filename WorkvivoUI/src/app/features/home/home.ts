import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { LocaleService } from '../../core/services/locale.service';

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HasPermissionDirective],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);

  readonly user = this.auth.user;
  readonly text = this.locale.text;

  readonly isAdmin = computed(() => this.user()?.isAdmin === true);

  readonly permissionCount = computed(() => this.user()?.permissions.length ?? 0);

  readonly rolesLabel = computed(() => this.user()?.roles.join(', ') || '-');
}
