import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AppNotification, NotificationType, RealtimeNotification } from '../models/notification';
import { MAX_COUNTED, NotificationService } from './notification.service';
import { RealtimeService } from './realtime.service';

/**
 * Stands in for the hub so the tests can push a notification without a socket.
 *
 * Also keeps the real service out of jsdom, where building a SignalR connection has
 * nothing to build on.
 */
class FakeRealtimeService {
  private readonly handlers = new Map<string, ((payload: unknown) => void)[]>();

  on<T>(eventName: string, handler: (payload: T) => void): void {
    const existing = this.handlers.get(eventName) ?? [];
    this.handlers.set(eventName, [...existing, handler as (payload: unknown) => void]);
  }

  /** Pretends the server pushed something. */
  emit(eventName: string, payload: unknown): void {
    for (const handler of this.handlers.get(eventName) ?? []) {
      handler(payload);
    }
  }
}

function notification(id: string, seen = false): AppNotification {
  return {
    id,
    notificationId: `n-${id}`,
    type: NotificationType.Mention,
    header: 'Layla mentioned you in a post',
    content: 'take a look',
    redirectUrl: '/feed/1',
    entityType: 1,
    entityId: '1',
    actorEmployeeId: null,
    actorDisplayName: 'Layla',
    actorProfilePictureFileId: null,
    isSeen: seen,
    createdDate: new Date().toISOString(),
  };
}

describe('NotificationService', () => {
  let service: NotificationService;
  let http: HttpTestingController;
  let realtime: FakeRealtimeService;

  beforeEach(() => {
    realtime = new FakeRealtimeService();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RealtimeService, useValue: realtime },
      ],
    });

    service = TestBed.inject(NotificationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows the unread count the API reports', () => {
    service.refreshUnread().subscribe();

    http
      .expectOne('/api/notifications/unread-count')
      .flush({ success: true, message: '', data: { unread: 4 } });

    expect(service.unread()).toBe(4);
    expect(service.badge()).toBe('4');
  });

  it('caps the badge rather than showing a four-digit number', () => {
    service.refreshUnread().subscribe();

    http
      .expectOne('/api/notifications/unread-count')
      .flush({ success: true, message: '', data: { unread: MAX_COUNTED + 1 } });

    expect(service.badge()).toBe(`${MAX_COUNTED}+`);
  });

  it('adds a pushed notification without asking the API for it again', () => {
    // The push carries the whole notification precisely so an announcement fan-out
    // does not have every connected client hit the API at the same moment.
    const payload: RealtimeNotification = { notification: notification('a'), unread: 1 };

    realtime.emit('notification', payload);

    expect(service.items().length).toBe(1);
    expect(service.unread()).toBe(1);

    // No request was made, which http.verify() in afterEach would otherwise catch.
  });

  it('puts the newest notification first', () => {
    realtime.emit('notification', { notification: notification('older'), unread: 1 });
    realtime.emit('notification', { notification: notification('newer'), unread: 2 });

    expect(service.items().map((item) => item.id)).toEqual(['newer', 'older']);
  });

  it('marks one notification read and takes it off the count', () => {
    realtime.emit('notification', { notification: notification('a'), unread: 1 });
    realtime.emit('notification', { notification: notification('b'), unread: 2 });

    service.markRead(['a']).subscribe();

    http
      .expectOne('/api/notifications/read')
      .flush({ success: true, message: '', data: 1 });

    expect(service.items().find((item) => item.id === 'a')?.isSeen).toBe(true);
    expect(service.items().find((item) => item.id === 'b')?.isSeen).toBe(false);
    expect(service.unread()).toBe(1);
  });

  it('marks everything read when no ids are given', () => {
    realtime.emit('notification', { notification: notification('a'), unread: 1 });
    realtime.emit('notification', { notification: notification('b'), unread: 2 });

    service.markRead([]).subscribe();

    http.expectOne('/api/notifications/read').flush({ success: true, message: '', data: 2 });

    expect(service.items().every((item) => item.isSeen)).toBe(true);
    expect(service.unread()).toBe(0);
  });

  it('never shows a negative count', () => {
    // Marking something read twice, or a stale badge, must not produce "-1 unread".
    service.markRead(['a', 'b', 'c']).subscribe();

    http.expectOne('/api/notifications/read').flush({ success: true, message: '', data: 0 });

    expect(service.unread()).toBe(0);
  });

  it('replaces the list on a first page and appends on later ones', () => {
    service.load(null).subscribe();

    http.expectOne((request) => request.url === '/api/notifications').flush({
      success: true,
      message: '',
      data: { items: [notification('a')], nextCursor: 'cursor-1', hasMore: true },
    });

    service.load('cursor-1').subscribe();

    http.expectOne((request) => request.url === '/api/notifications').flush({
      success: true,
      message: '',
      data: { items: [notification('b')], nextCursor: null, hasMore: false },
    });

    expect(service.items().map((item) => item.id)).toEqual(['a', 'b']);
  });
});
