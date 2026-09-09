import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { routes } from '../../app.routes';
import { EN } from '../i18n/messages';
import { successBody } from '../testing/session-fixture';

describe('route protection', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /**
   * The guards ask the API whether the refresh cookie is still good, because the access
   * token is never persisted - after a reload there is nothing local to inspect.
   */
  async function answerRestore(signedIn: boolean): Promise<void> {
    // The guard issues its request asynchronously, so the test has to yield before the
    // testing backend has anything to match.
    await new Promise((resolve) => setTimeout(resolve, 0));

    const request = http.expectOne('/api/auth/refresh');

    if (signedIn) {
      request.flush(successBody());
    } else {
      request.flush(null, { status: 401, statusText: 'Unauthorized' });
    }
  }

  it('sends a signed-out visitor from /home to /login and remembers where they wanted to go', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/home');

    await answerRestore(false);
    await navigation;

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fhome');
  });

  it('lets a reloading user back into /home using only the refresh cookie', async () => {
    // The point of the design: the session survives a reload without the access token
    // ever having been written anywhere script can read.
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/home');

    await answerRestore(true);
    await navigation;

    expect(TestBed.inject(Router).url).toBe('/home');
  });

  it('keeps a signed-in user off /login', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/login');

    await answerRestore(true);
    await navigation;

    expect(TestBed.inject(Router).url).toBe('/home');
  });

  it('serves the public page with no session and without asking the API', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/public');

    // No guard on this route, so nothing should be probing the session.
    http.expectNone('/api/auth/refresh');

    expect(TestBed.inject(Router).url).toBe('/public');
    expect(harness.routeNativeElement?.textContent).toContain(EN.publicPage.eyebrow);
  });
});
