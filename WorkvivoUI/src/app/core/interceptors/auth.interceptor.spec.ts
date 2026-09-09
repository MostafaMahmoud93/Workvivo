import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { successBody } from '../testing/session-fixture';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => http.verify());

  /** Signs in through the real service so the token lands where the interceptor reads it. */
  function signIn(): void {
    auth.login({ userName: 'dev', password: 'x' }).subscribe();
    http.expectOne('/api/auth/login').flush(successBody());
  }

  it('leaves an unauthenticated request alone', () => {
    client.get('/api/posts').subscribe();

    expect(http.expectOne('/api/posts').request.headers.has('Authorization')).toBe(false);
  });

  it('attaches the bearer token once signed in', () => {
    signIn();
    client.get('/api/posts').subscribe();

    expect(http.expectOne('/api/posts').request.headers.get('Authorization'))
      .toBe('Bearer issued-token');
  });

  it('refreshes and replays the original request on a 401', () => {
    signIn();

    let body: unknown;
    client.get('/api/posts').subscribe((response) => (body = response));

    // The access token has expired.
    http.expectOne('/api/posts').flush(null, { status: 401, statusText: 'Unauthorized' });

    // One refresh...
    http.expectOne('/api/auth/refresh').flush({
      success: true,
      message: '',
      data: { ...successBody().data, accessToken: 'rotated-token' },
    });

    // ...then the original request again, carrying the new token.
    const replay = http.expectOne('/api/posts');
    expect(replay.request.headers.get('Authorization')).toBe('Bearer rotated-token');

    replay.flush({ ok: true });
    expect(body).toEqual({ ok: true });
  });

  it('issues a single refresh for several requests that expire together', () => {
    signIn();

    client.get('/api/posts').subscribe({ error: () => undefined });
    client.get('/api/events').subscribe({ error: () => undefined });
    client.get('/api/notifications').subscribe({ error: () => undefined });

    for (const url of ['/api/posts', '/api/events', '/api/notifications']) {
      http.expectOne(url).flush(null, { status: 401, statusText: 'Unauthorized' });
    }

    // Exactly one. A second concurrent refresh would present an already-rotated token,
    // trip the server's replay detection, and revoke the whole family - signing the
    // user out because their session was working too hard.
    const refreshes = http.match('/api/auth/refresh');
    expect(refreshes.length).toBe(1);

    refreshes[0].flush(successBody());

    for (const url of ['/api/posts', '/api/events', '/api/notifications']) {
      http.expectOne(url).flush({});
    }
  });

  it('does not try to refresh a failed refresh', () => {
    signIn();

    client.get('/api/posts').subscribe({ error: () => undefined });
    http.expectOne('/api/posts').flush(null, { status: 401, statusText: 'Unauthorized' });

    // The refresh itself fails: the session is genuinely over.
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    // No retry storm, and no second refresh.
    http.expectNone('/api/auth/refresh');
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('does not attempt a refresh when the login itself is rejected', () => {
    auth.login({ userName: 'dev', password: 'wrong' }).subscribe({ error: () => undefined });

    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    http.expectNone('/api/auth/refresh');
  });
});
