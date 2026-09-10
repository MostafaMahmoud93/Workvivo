import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { EmployeeService } from '../../../core/services/employee.service';
import { LocaleService } from '../../../core/services/locale.service';
import { EmployeeProfile as Profile } from '../../../core/models/employee';
import { Avatar } from '../../../shared/components/avatar/avatar';

@Component({
  selector: 'app-employee-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Avatar],
  templateUrl: './employee-profile.html',
  styleUrl: './employee-profile.scss',
})
export class EmployeeProfilePage {
  private readonly employees = inject(EmployeeService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  /** Bound from the route. Absent on /profile, which shows the caller's own page. */
  readonly employeeId = input<string | undefined>(undefined);

  readonly profile = signal<Profile | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly followBusy = signal(false);

  readonly joinedLabel = computed(() => {
    const joined = this.profile()?.joiningDate;
    if (!joined) {
      return null;
    }
    return new Date(joined).toLocaleDateString(this.locale.locale(), {
      year: 'numeric',
      month: 'long',
    });
  });

  /**
   * Day and month only. The API never sends a birth year, so a fixed placeholder year
   * is used purely to get the locale's day-and-month formatting.
   */
  readonly birthdayLabel = computed(() => {
    const current = this.profile();
    if (!current?.birthDay || !current.birthMonth) {
      return null;
    }
    return new Date(2000, current.birthMonth - 1, current.birthDay).toLocaleDateString(
      this.locale.locale(),
      { day: 'numeric', month: 'long' },
    );
  });

  constructor() {
    effect(() => {
      const id = this.employeeId();
      this.load(id);
    });
  }

  toggleFollow(): void {
    const current = this.profile();
    if (!current || this.followBusy()) {
      return;
    }

    const next = !current.isFollowedByMe;
    this.followBusy.set(true);

    // Applied optimistically so the button responds immediately, and rolled back on
    // failure - a follow is cheap and almost always succeeds, and waiting a round trip
    // to redraw a toggle feels broken.
    this.profile.set({
      ...current,
      isFollowedByMe: next,
      followersCount: current.followersCount + (next ? 1 : -1),
    });

    this.employees.setFollowing(current.id, next).subscribe({
      next: () => this.followBusy.set(false),
      error: () => {
        this.profile.set(current);
        this.followBusy.set(false);
      },
    });
  }

  private load(employeeId: string | undefined): void {
    this.loading.set(true);
    this.failed.set(false);

    this.employees.getProfile(employeeId).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
