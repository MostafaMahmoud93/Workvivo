import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  CommunityInvitation,
  CommunityPrivacy,
  CommunitySummary,
  MembershipStatus,
} from '../../../core/models/community';
import { CommunityService } from '../../../core/services/community.service';
import { LocaleService } from '../../../core/services/locale.service';
import { Avatar } from '../../../shared/components/avatar/avatar';

/**
 * The community directory.
 *
 * Two tabs over one query rather than two endpoints: "mine" is a filter on the same
 * list, and keeping it that way means the join state shown in each tab is produced by
 * the same code.
 */
@Component({
  selector: 'app-community-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, Avatar],
  templateUrl: './community-list.html',
  styleUrl: './community-list.scss',
})
export class CommunityList {
  private readonly communities = inject(CommunityService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly privacy = CommunityPrivacy;
  readonly status = MembershipStatus;

  readonly scope = signal<'Discover' | 'Mine'>('Discover');
  readonly search = signal('');
  readonly items = signal<CommunitySummary[]>([]);
  readonly invitations = signal<CommunityInvitation[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal<string | null>(null);

  readonly isEmpty = computed(() => !this.loading() && this.items().length === 0);

  constructor() {
    this.load();
    this.loadInvitations();
  }

  setScope(scope: 'Discover' | 'Mine'): void {
    if (this.scope() === scope) {
      return;
    }

    this.scope.set(scope);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.communities.list(this.scope(), this.search()).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  join(community: CommunitySummary): void {
    if (this.busy()) {
      return;
    }

    this.busy.set(community.id);

    this.communities.join(community.id).subscribe({
      next: (result) => {
        // The server decides whether this was a join or a request - a Restricted
        // community looks identical from here until it answers.
        this.patch(community.id, (current) => ({
          ...current,
          myMembershipStatus: result,
          canJoin: false,
          membersCount:
            result === MembershipStatus.Approved ? current.membersCount + 1 : current.membersCount,
        }));

        this.busy.set(null);
      },
      error: () => this.busy.set(null),
    });
  }

  leave(community: CommunitySummary): void {
    if (this.busy()) {
      return;
    }

    this.busy.set(community.id);

    this.communities.leave(community.id).subscribe({
      next: () => {
        this.patch(community.id, (current) => ({
          ...current,
          myMembershipStatus: MembershipStatus.Left,
          canJoin: true,
          membersCount: Math.max(0, current.membersCount - 1),
        }));

        this.busy.set(null);
      },
      error: () => this.busy.set(null),
    });
  }

  respond(invitation: CommunityInvitation, accept: boolean): void {
    this.communities.respondToInvitation(invitation.id, accept).subscribe({
      next: () => {
        this.invitations.update((current) => current.filter((item) => item.id !== invitation.id));

        if (accept) {
          this.load();
        }
      },
    });
  }

  privacyLabel(value: CommunityPrivacy): string {
    return this.text().communities.privacy[value] ?? '';
  }

  private loadInvitations(): void {
    this.communities.myInvitations().subscribe({
      next: (invitations) => this.invitations.set(invitations),
      error: () => this.invitations.set([]),
    });
  }

  private patch(id: string, change: (community: CommunitySummary) => CommunitySummary): void {
    this.items.update((items) => items.map((item) => (item.id === id ? change(item) : item)));
  }
}
