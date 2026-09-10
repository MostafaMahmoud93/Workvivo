import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  DigestFrequency,
  NotificationPreference,
  NotificationType,
} from '../../../core/models/notification';
import { LocaleService } from '../../../core/services/locale.service';
import { NotificationService } from '../../../core/services/notification.service';

/**
 * Per-type notification preferences.
 *
 * Edited locally and saved in one request rather than one per toggle. Somebody turning
 * six things off should not generate six writes, and a half-applied set of preferences
 * is a confusing state to leave a person in if the network drops midway.
 */
@Component({
  selector: 'app-notification-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './notification-settings.html',
  styleUrl: './notification-settings.scss',
})
export class NotificationSettings {
  private readonly notifications = inject(NotificationService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  readonly rows = signal<NotificationPreference[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly failed = signal(false);

  /** The digest choices, in the order they read as increasing delay. */
  readonly frequencies: DigestFrequency[] = [
    DigestFrequency.Immediate,
    DigestFrequency.Hourly,
    DigestFrequency.Daily,
    DigestFrequency.Weekly,
    DigestFrequency.Never,
  ];

  readonly isEmpty = computed(() => !this.loading() && this.rows().length === 0);

  constructor() {
    this.notifications.preferences().subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  typeLabel(type: NotificationType): string {
    // Indexed rather than switched, so adding a type to the enum surfaces as a missing
    // key in the catalogue instead of falling through a default case unnoticed.
    return this.text().notificationTypes[type] ?? String(type);
  }

  frequencyLabel(frequency: DigestFrequency): string {
    return this.text().digestFrequencies[frequency] ?? String(frequency);
  }

  setInApp(type: NotificationType, value: boolean): void {
    this.patch(type, (row) => ({ ...row, inApp: value }));
  }

  setEmail(type: NotificationType, value: boolean): void {
    this.patch(type, (row) => ({ ...row, email: value }));
  }

  setFrequency(type: NotificationType, value: string): void {
    this.patch(type, (row) => ({ ...row, emailFrequency: Number(value) as DigestFrequency }));
  }

  save(): void {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.saved.set(false);
    this.failed.set(false);

    this.notifications.savePreferences(this.rows()).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
      },
      error: () => {
        this.saving.set(false);
        this.failed.set(true);
      },
    });
  }

  private patch(type: NotificationType, change: (row: NotificationPreference) => NotificationPreference): void {
    this.saved.set(false);
    this.rows.update((rows) => rows.map((row) => (row.type === type ? change(row) : row)));
  }
}
