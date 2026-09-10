import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import {
  AppNotification,
  DEFAULT_NOTIFICATION_GLYPH,
  NOTIFICATION_GLYPHS,
  NotificationType,
} from '../../../core/models/notification';
import { LocaleService } from '../../../core/services/locale.service';
import { NotificationService } from '../../../core/services/notification.service';

/**
 * The bell in the header, and the panel behind it.
 *
 * The unread count is loaded once at start-up and then kept current by the hub, so
 * nothing here polls. If the connection is down the count goes stale until the panel
 * is opened, which refetches - a deliberate trade against a request every thirty
 * seconds from every open tab in the company.
 */
@Component({
  selector: 'app-notification-bell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.scss',
})
export class NotificationBell {
  private readonly notifications = inject(NotificationService);
  private readonly locale = inject(LocaleService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly text = this.locale.text;

  readonly unread = this.notifications.unread;
  readonly badge = this.notifications.badge;
  readonly items = this.notifications.items;

  readonly open = signal(false);
  readonly loading = signal(false);

  constructor() {
    this.notifications.refreshUnread().subscribe({
      // A failure here must not break the header. The bell simply shows nothing until
      // the next push or the next time the panel is opened.
      error: () => undefined,
    });
  }

  glyphFor(type: NotificationType): string {
    return NOTIFICATION_GLYPHS[type] ?? DEFAULT_NOTIFICATION_GLYPH;
  }

  toggle(): void {
    const next = !this.open();
    this.open.set(next);

    if (next) {
      this.load();
    }
  }

  /** Clicking outside closes the panel - the usual behaviour for a popover. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }

  activate(notification: AppNotification): void {
    if (!notification.isSeen) {
      this.notifications.markRead([notification.id]).subscribe({ error: () => undefined });
    }

    this.open.set(false);

    if (notification.redirectUrl) {
      // The stored link is a client path with an optional query string, which is what
      // the router wants split apart rather than handed over whole.
      const [path, query] = notification.redirectUrl.split('?');
      const params = Object.fromEntries(new URLSearchParams(query ?? ''));

      void this.router.navigate([path], { queryParams: params });
    }
  }

  markAllRead(): void {
    this.notifications.markRead([]).subscribe({ error: () => undefined });
  }

  when(iso: string): string {
    const minutes = Math.round((Date.now() - new Date(iso).getTime()) / 60000);

    if (minutes < 1) return this.text().notifications.justNow;
    if (minutes < 60) return `${minutes}m`;
    if (minutes < 1440) return `${Math.round(minutes / 60)}h`;

    return new Date(iso).toLocaleDateString(this.locale.locale(), {
      day: 'numeric',
      month: 'short',
    });
  }

  private load(): void {
    this.loading.set(true);

    this.notifications.load(null).subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false),
    });
  }
}
