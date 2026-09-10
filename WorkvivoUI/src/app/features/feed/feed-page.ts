import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { FeedService } from '../../core/services/feed.service';
import { LocaleService } from '../../core/services/locale.service';
import { AuthService } from '../../core/services/auth.service';
import {
  Comment,
  FeedItem,
  REACTION_GLYPHS,
  REACTION_TYPES,
  ReactionType,
} from '../../core/models/feed';
import { Avatar } from '../../shared/components/avatar/avatar';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-feed-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Avatar, HasPermissionDirective],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
})
export class FeedPage {
  private readonly feed = inject(FeedService);
  private readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly domSanitizer = inject(DomSanitizer);

  readonly text = this.locale.text;
  readonly reactionTypes = REACTION_TYPES;
  readonly glyphs = REACTION_GLYPHS;
  readonly skeletons = Array.from({ length: 3 });

  readonly posts = signal<FeedItem[]>([]);
  readonly cursor = signal<string | null>(null);
  readonly hasMore = signal(false);
  readonly loading = signal(true);
  readonly loadingMore = signal(false);
  readonly failed = signal(false);

  readonly composerText = signal('');
  readonly posting = signal(false);

  /** Post id whose thread is open, or null. One at a time keeps the page readable. */
  readonly openThread = signal<string | null>(null);
  readonly comments = signal<Comment[]>([]);
  readonly commentsLoading = signal(false);
  readonly replyText = signal('');
  readonly replyingTo = signal<string | null>(null);

  readonly isEmpty = computed(
    () => !this.loading() && !this.failed() && this.posts().length === 0,
  );

  constructor() {
    this.load(null);
  }

  /**
   * The API sanitises on write, so what arrives has already been through the
   * allow-list; this only tells Angular not to strip it a second time. Marking
   * unsanitised input trusted would be an XSS hole - the server is what makes this
   * safe, not the call itself.
   */
  trustedHtml(html: string | null): SafeHtml {
    return this.domSanitizer.bypassSecurityTrustHtml(html ?? '');
  }

  when(iso: string): string {
    const then = new Date(iso).getTime();
    const minutes = Math.round((Date.now() - then) / 60000);

    if (minutes < 1) return this.text().feed.justNow;
    if (minutes < 60) return `${minutes}m`;
    if (minutes < 1440) return `${Math.round(minutes / 60)}h`;

    return new Date(iso).toLocaleDateString(this.locale.locale(), {
      day: 'numeric',
      month: 'short',
    });
  }

  reactionCount(post: FeedItem, reaction: ReactionType): number {
    return post.reactions[reaction] ?? 0;
  }

  react(post: FeedItem, reaction: ReactionType): void {
    // Tapping the reaction you already hold removes it.
    const next = post.myReaction === reaction ? null : reaction;
    const previous = { ...post };

    // Applied straight away and rolled back on failure: waiting a round trip to redraw
    // a toggle makes the whole feed feel unresponsive.
    this.patch(post.id, (current) => {
      const counts = { ...current.reactions };

      if (current.myReaction) {
        counts[current.myReaction] = Math.max(0, (counts[current.myReaction] ?? 1) - 1);
      }
      if (next) {
        counts[next] = (counts[next] ?? 0) + 1;
      }

      const delta = (next ? 1 : 0) - (current.myReaction ? 1 : 0);

      return {
        ...current,
        myReaction: next,
        reactions: counts,
        reactionsCount: Math.max(0, current.reactionsCount + delta),
      };
    });

    this.feed.react(post.id, next).subscribe({
      next: (total) => this.patch(post.id, (current) => ({ ...current, reactionsCount: total })),
      error: () => this.patch(post.id, () => previous),
    });
  }

  toggleThread(post: FeedItem): void {
    if (this.openThread() === post.id) {
      this.openThread.set(null);
      return;
    }

    this.openThread.set(post.id);
    this.comments.set([]);
    this.replyingTo.set(null);
    this.commentsLoading.set(true);

    this.feed.getComments(post.id, null).subscribe({
      next: (page) => {
        this.comments.set(page.items);
        this.commentsLoading.set(false);
      },
      error: () => this.commentsLoading.set(false),
    });
  }

  submitComment(post: FeedItem): void {
    const body = this.replyText().trim();
    if (!body) {
      return;
    }

    const parentId = this.replyingTo() ?? undefined;

    this.feed.addComment(post.id, `<p>${escapeHtml(body)}</p>`, parentId).subscribe({
      next: () => {
        this.replyText.set('');
        this.replyingTo.set(null);
        this.patch(post.id, (current) => ({ ...current, commentsCount: current.commentsCount + 1 }));

        this.feed.getComments(post.id, null).subscribe((page) => this.comments.set(page.items));
      },
    });
  }

  startReply(comment: Comment): void {
    this.replyingTo.set(comment.id);
  }

  cancelReply(): void {
    this.replyingTo.set(null);
  }

  publish(): void {
    const body = this.composerText().trim();
    if (!body || this.posting()) {
      return;
    }

    this.posting.set(true);

    this.feed.createPost(`<p>${escapeHtml(body)}</p>`, null).subscribe({
      next: () => {
        this.composerText.set('');
        this.posting.set(false);
        // Reloaded rather than prepended: the new post has to pass the same audience
        // rules as everything else, and the server is what decides that.
        this.load(null);
      },
      error: () => this.posting.set(false),
    });
  }

  loadMore(): void {
    if (!this.hasMore() || this.loadingMore()) {
      return;
    }

    this.loadingMore.set(true);
    this.load(this.cursor());
  }

  private load(cursor: string | null): void {
    if (cursor === null) {
      this.loading.set(true);
    }
    this.failed.set(false);

    this.feed.getFeed(cursor).subscribe({
      next: (page) => {
        this.posts.update((current) => (cursor === null ? page.items : [...current, ...page.items]));
        this.cursor.set(page.nextCursor);
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
        this.loadingMore.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
        this.loadingMore.set(false);
      },
    });
  }

  private patch(postId: string, change: (post: FeedItem) => FeedItem): void {
    this.posts.update((posts) => posts.map((post) => (post.id === postId ? change(post) : post)));
  }
}

/** The composer is plain text; this stops a typed angle bracket becoming markup. */
function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}
