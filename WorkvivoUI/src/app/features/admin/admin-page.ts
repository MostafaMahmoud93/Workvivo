import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuditEntry, Role } from '../../core/models/admin';
import { AdminService } from '../../core/services/admin.service';
import { LocaleService } from '../../core/services/locale.service';

/**
 * The administration screen: roles, and the audit trail.
 *
 * Read-oriented on purpose. Granting permissions and editing the organisation are
 * done through the existing screens, each behind its own permission - collecting
 * every dangerous action onto one page makes it easy to grant somebody "admin" and
 * hard to say what that means.
 */
@Component({
  selector: 'app-admin-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss',
})
export class AdminPage {
  private readonly admin = inject(AdminService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  readonly tab = signal<'roles' | 'audit'>('roles');
  readonly roles = signal<Role[]>([]);
  readonly audit = signal<AuditEntry[]>([]);
  readonly entityFilter = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly loading = signal(true);
  readonly failed = signal(false);

  constructor() {
    this.loadRoles();
  }

  setTab(tab: 'roles' | 'audit'): void {
    if (this.tab() === tab) {
      return;
    }

    this.tab.set(tab);

    if (tab === 'audit' && this.audit().length === 0) {
      this.loadAudit(1);
    }
  }

  loadAudit(page: number): void {
    this.loading.set(true);
    this.failed.set(false);

    this.admin.auditLog(page, this.entityFilter()).subscribe({
      next: (result) => {
        this.audit.set(result.items);
        this.page.set(result.pageNumber);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  when(iso: string): string {
    return new Date(iso).toLocaleString(this.locale.locale());
  }

  private loadRoles(): void {
    this.loading.set(true);

    this.admin.roles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
