import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import {
  CommunityDetail,
  CommunityMember,
  CommunityMemberRole,
  CommunityPrivacy,
  MembershipDecision,
  MembershipStatus,
} from '../../../core/models/community';
import { CommunityService } from '../../../core/services/community.service';
import { LocaleService } from '../../../core/services/locale.service';
import { Avatar } from '../../../shared/components/avatar/avatar';

/**
 * One community: who is in it, and - for a moderator - who wants in.
 *
 * The buttons are driven by the permission flags on the response rather than by the
 * client working them out from privacy and role. A client that re-derives them will
 * eventually disagree with the server, and the disagreement always surfaces as a
 * confusing error at the point of use.
 */
@Component({
  selector: 'app-community-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Avatar],
  templateUrl: './community-detail.html',
  styleUrl: './community-detail.scss',
})
export class CommunityDetailPage {
  private readonly communities = inject(CommunityService);
  private readonly locale = inject(LocaleService);

  /** Bound from the route by withComponentInputBinding. */
  readonly communityId = input.required<string>();

  readonly text = this.locale.text;
  readonly privacy = CommunityPrivacy;
  readonly roles = CommunityMemberRole;
  readonly status = MembershipStatus;

  readonly community = signal<CommunityDetail | null>(null);
  readonly members = signal<CommunityMember[]>([]);
  readonly pending = signal<CommunityMember[]>([]);
  readonly tab = signal<'members' | 'requests'>('members');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);

  constructor() {
    effect(() => {
      const id = this.communityId();

      if (id) {
        this.load(id);
      }
    });
  }

  setTab(tab: 'members' | 'requests'): void {
    this.tab.set(tab);
  }

  join(): void {
    const current = this.community();

    if (!current || this.busy()) {
      return;
    }

    this.busy.set(true);

    this.communities.join(current.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.load(current.id);
      },
      error: () => this.busy.set(false),
    });
  }

  leave(): void {
    const current = this.community();

    if (!current || this.busy()) {
      return;
    }

    this.busy.set(true);

    this.communities.leave(current.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.load(current.id);
      },
      error: () => this.busy.set(false),
    });
  }

  review(member: CommunityMember, decision: MembershipDecision): void {
    const current = this.community();

    if (!current) {
      return;
    }

    this.communities.review(current.id, member.employeeId, decision).subscribe({
      next: () => this.load(current.id),
    });
  }

  setRole(member: CommunityMember, role: CommunityMemberRole): void {
    const current = this.community();

    if (!current) {
      return;
    }

    this.communities.setRole(current.id, member.employeeId, role).subscribe({
      next: () => this.load(current.id),
    });
  }

  roleLabel(role: CommunityMemberRole): string {
    return this.text().communities.roles[role] ?? '';
  }

  privacyLabel(value: CommunityPrivacy): string {
    return this.text().communities.privacy[value] ?? '';
  }

  private load(id: string): void {
    this.loading.set(true);
    this.failed.set(false);

    this.communities.detail(id).subscribe({
      next: (community) => {
        this.community.set(community);
        this.loading.set(false);

        // Members are only fetched when they can be seen. Asking anyway would produce
        // a 403 in the console on every visit to a Restricted community.
        if (community.canRead) {
          this.loadMembers(id);
        } else {
          this.members.set([]);
        }

        if (community.canModerate) {
          this.loadPending(id);
        } else {
          this.pending.set([]);
        }
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  private loadMembers(id: string): void {
    this.communities.members(id, false).subscribe({
      next: (page) => this.members.set(page.items),
      error: () => this.members.set([]),
    });
  }

  private loadPending(id: string): void {
    this.communities.members(id, true).subscribe({
      next: (page) => this.pending.set(page.items),
      error: () => this.pending.set([]),
    });
  }
}
