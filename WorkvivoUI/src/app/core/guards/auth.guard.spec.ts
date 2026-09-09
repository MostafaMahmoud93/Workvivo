import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { routes } from '../../app.routes';
import { EN } from '../i18n/messages';

function storeValidSession(): void {
  localStorage.setItem(
    'workvivo.session',
    JSON.stringify({
      token: 'test-token',
      expiration: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      userId: '11111111-1111-1111-1111-111111111111',
      isAdmin: true,
      userType: 'MANAG',
      userActions: ['api/User/GetUsers'],
    }),
  );
}

describe('route protection', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('sends a signed-out visitor from /home to /login and remembers where they wanted to go', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home');

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fhome');
  });

  it('lets a signed-in user reach /home', async () => {
    storeValidSession();

    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home');

    expect(TestBed.inject(Router).url).toBe('/home');
  });

  it('keeps a signed-in user off /login', async () => {
    storeValidSession();

    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login');

    expect(TestBed.inject(Router).url).toBe('/home');
  });

  it('serves the public page with no session', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/public');

    expect(TestBed.inject(Router).url).toBe('/public');
    // Asserted against the i18n source so rewording the page copy
    // cannot fail a test about route protection.
    expect(harness.routeNativeElement?.textContent).toContain(EN.publicPage.eyebrow);
  });

  it('treats an expired token as signed out', async () => {
    localStorage.setItem(
      'workvivo.session',
      JSON.stringify({
        token: 'stale-token',
        expiration: new Date(Date.now() - 1000).toISOString(),
        userId: null,
        isAdmin: false,
        userType: null,
        userActions: [],
      }),
    );

    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home');

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fhome');
  });
});
