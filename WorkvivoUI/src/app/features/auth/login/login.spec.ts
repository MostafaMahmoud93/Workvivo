import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';

import { Login } from './login';
import { routes } from '../../../app.routes';
import { AuthService } from '../../../core/services/auth.service';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
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

    http.expectNone('/api/Auth/Login');
    expect(fixture.componentInstance.form.controls.userName.touched).toBe(true);
  });

  it('posts the credentials and stores the session on success', () => {
    fillAndSubmit();

    const request = http.expectOne('/api/Auth/Login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ userName: 'dev', password: 'P@55w0rd' });

    request.flush({
      success: true,
      message: '',
      data: {
        token: 'issued-token',
        expiration: new Date(Date.now() + 3600_000).toISOString(),
        userId: '11111111-1111-1111-1111-111111111111',
        isAdmin: true,
        userType: 'MANAG',
        userActions: [],
      },
    });

    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(true);
    expect(fixture.componentInstance.error()).toBeNull();
  });

  it('shows the message when the API answers HTTP 200 with success:false', () => {
    fillAndSubmit();

    http.expectOne('/api/Auth/Login').flush({
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

    http.expectOne('/api/Auth/Login').error(new ProgressEvent('error'));

    expect(fixture.componentInstance.error()).toContain('Could not reach Workvivo.API');
    expect(fixture.componentInstance.busy()).toBe(false);
  });

  it('sends the signed-in user on to /home', async () => {
    fillAndSubmit();

    http.expectOne('/api/Auth/Login').flush({
      success: true,
      message: '',
      data: {
        token: 'issued-token',
        expiration: new Date(Date.now() + 3600_000).toISOString(),
        userId: null,
        isAdmin: false,
        userType: 'PORTA',
        userActions: [],
      },
    });

    await fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/home');
  });
});
