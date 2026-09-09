import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';

import { Login } from './login';
import { routes } from '../../../app.routes';
import { AuthService } from '../../../core/services/auth.service';
import { successBody } from '../../../core/testing/session-fixture';

const LOGIN_URL = '/api/auth/login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  function fillAndSubmit(): void {
    fixture.componentInstance.form.setValue({ userName: 'dev', password: 'P@55w0rd' });
    fixture.componentInstance.submit();
  }

  it('does not call the API until both fields are filled', () => {
    fixture.componentInstance.submit();

    http.expectNone(LOGIN_URL);
    expect(fixture.componentInstance.form.controls.userName.touched).toBe(true);
  });

  it('posts the credentials with credentials enabled so the refresh cookie is accepted', () => {
    fillAndSubmit();

    const request = http.expectOne(LOGIN_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ userName: 'dev', password: 'P@55w0rd' });

    // Without withCredentials the browser discards the Set-Cookie carrying the refresh
    // token: sign-in appears to work and every later refresh fails.
    expect(request.request.withCredentials).toBe(true);

    request.flush(successBody());

    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(true);
  });

  it('keeps the access token out of browser storage', () => {
    fillAndSubmit();
    http.expectOne(LOGIN_URL).flush(successBody());

    // The token lives in a signal and nowhere else. Anything readable by script is
    // readable by an XSS payload.
    expect(JSON.stringify(localStorage)).not.toContain('issued-token');
    expect(JSON.stringify(sessionStorage)).not.toContain('issued-token');
    expect(TestBed.inject(AuthService).accessToken()).toBe('issued-token');
  });

  it('exposes the permissions the API reported', () => {
    fillAndSubmit();
    http.expectOne(LOGIN_URL).flush(successBody(['Post.Create', 'Post.View']));

    const auth = TestBed.inject(AuthService);
    expect(auth.has('Post.Create')).toBe(true);
    expect(auth.has('Role.Manage')).toBe(false);
    expect(auth.hasAny('Role.Manage', 'Post.View')).toBe(true);
  });

  it('shows the message when the API answers HTTP 200 with success:false', () => {
    fillAndSubmit();

    http.expectOne(LOGIN_URL).flush({
      success: false,
      message: 'Invalid username or password.',
      data: null,
    });

    expect(fixture.componentInstance.error()).toBe('Invalid username or password.');
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
    expect(fixture.componentInstance.busy()).toBe(false);
  });

  it('reports an unreachable API instead of hanging', () => {
    fillAndSubmit();

    http.expectOne(LOGIN_URL).error(new ProgressEvent('error'));

    expect(fixture.componentInstance.error()).toContain('Could not reach Workvivo.API');
    expect(fixture.componentInstance.busy()).toBe(false);
  });

  it('sends the signed-in user on to /home', async () => {
    fillAndSubmit();
    http.expectOne(LOGIN_URL).flush(successBody());

    await fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/home');
  });
});
