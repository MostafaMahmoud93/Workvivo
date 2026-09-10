import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CursorPage } from '../models/feed';
import {
  AppNotification,
  NotificationPreference,
  RealtimeNotification,
} from '../models/notification';
import { ServiceResponse } from '../models/service-response';
import { RealtimeService } from './realtime.service';

/** Matches the server's cap; past this the badge reads "99+". */
export const MAX_COUNTED = 99;

/**
 * The bell, and the list behind it.
 *
 * Holds the unread count as state because several places show it and they must not
 * each poll for it. The count moves for three reasons: a push arrives, the user marks
 * something read, or a fetch tells us what it really is - and the first two adjust it
 * locally so the UI responds immediately.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly realtime = inject(RealtimeService);
  private readonly base = environment.apiBaseUrl + '/api/notifications';

  private readonly _unread = signal(0);
  private readonly _items = signal<AppNotification[]>([]);

  readonly unread = this._unread.asReadonly();
  readonly items = this._items.asReadonly();

  /** What the badge shows. */
  readonly badge = computed(() => {
    const count = this._unread();
    return count > MAX_COUNTED ? `${MAX_COUNTED}+` : String(count);
  });

  constructor() {
    this.realtime.on<RealtimeNotification>('notification', (payload) => {
      // Prepended rather than refetched. The push carries the whole notification
      // precisely so a company-wide announcement does not have every connected client
      // hit the API at the same moment.
      this._items.update((current) => [payload.notification, ...current]);
      this._unread.update((count) => count + 1);
    });

    // Sent when something changed in bulk - an announcement fan-out - where the client
    // is better off asking for the real number than guessing.
    this.realtime.on('unreadCount', () => this.refreshUnread().subscribe());
  }

  refreshUnread(): Observable<number> {
    return this.http
      .get<ServiceResponse<{ unread: number }>>(this.base + '/unread-count')
      .pipe(
        map((response) => response.data.unread),
        tap((unread) => this._unread.set(unread)),
      );
  }

  load(cursor: string | null, unreadOnly = false): Observable<CursorPage<AppNotification>> {
    let params = new HttpParams().set('pageSize', 20);

    if (cursor) {
      params = params.set('cursor', cursor);
    }

    if (unreadOnly) {
      params = params.set('unreadOnly', true);
    }

    return this.http
      .get<ServiceResponse<CursorPage<AppNotification>>>(this.base, { params })
      .pipe(
        map((response) => response.data),
        tap((page) => {
          this._items.update((current) => (cursor === null ? page.items : [...current, ...page.items]));
        }),
      );
  }

  /** An empty list means every unread one. */
  markRead(ids: string[]): Observable<number> {
    return this.http.post<ServiceResponse<number>>(this.base + '/read', { ids }).pipe(
      map((response) => response.data),
      tap(() => {
        const target = new Set(ids);
        const markAll = ids.length === 0;

        this._items.update((current) =>
          current.map((item) =>
            markAll || target.has(item.id) ? { ...item, isSeen: true } : item,
          ),
        );

        // Recomputed from what is now in the list rather than subtracted. Subtracting
        // drifts as soon as a notification is marked read twice, and the list is the
        // only thing the user can actually see.
        this._unread.update((count) =>
          markAll ? 0 : Math.max(0, count - ids.length),
        );
      }),
    );
  }

  preferences(): Observable<NotificationPreference[]> {
    return this.http
      .get<ServiceResponse<NotificationPreference[]>>(this.base + '/preferences')
      .pipe(map((response) => response.data));
  }

  savePreferences(preferences: NotificationPreference[]): Observable<number> {
    return this.http
      .put<ServiceResponse<number>>(this.base + '/preferences', {
        preferences: preferences.map((preference) => ({
          type: preference.type,
          inApp: preference.inApp,
          email: preference.email,
          emailFrequency: preference.emailFrequency,
        })),
      })
      .pipe(map((response) => response.data));
  }
}
