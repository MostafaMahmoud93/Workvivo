import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginModel } from '../models/login-model';
import { ServiceResponse } from '../models/service-response';
import { TokenModel } from '../models/token-model';

const STORAGE_KEY = 'workvivo.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _session = signal<TokenModel | null>(readStoredSession());

  readonly session = this._session.asReadonly();

  readonly isAuthenticated = computed(() => {
    const session = this._session();
    if (!session?.token) {
      return false;
    }
    return new Date(session.expiration).getTime() > Date.now();
  });

  readonly isAdmin = computed(() => this._session()?.isAdmin === true);

  /** POST api/Auth/Login. Returns ServiceResponse of TokenModel. */
  login(model: LoginModel): Observable<ServiceResponse<TokenModel>> {
    return this.http
      .post<ServiceResponse<TokenModel>>(environment.apiBaseUrl + '/api/Auth/Login', model)
      .pipe(
        tap((response) => {
          // The API answers HTTP 200 with success:false for a business
          // rejection, so the flag decides - not the status code.
          if (response.success && response.data?.token) {
            this.store(response.data);
          }
        }),
      );
  }

  logout(): void {
    this._session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Private mode / blocked site data - nothing to clear.
    }
    void this.router.navigate(['/login']);
  }

  private store(session: TokenModel): void {
    this._session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Token stays in memory for this tab only.
    }
  }
}

function readStoredSession(): TokenModel | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as TokenModel;
    return parsed?.token ? parsed : null;
  } catch {
    return null;
  }
}
