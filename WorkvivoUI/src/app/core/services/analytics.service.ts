import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AnalyticsDashboard } from '../models/analytics';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/analytics';

  dashboard(days: number): Observable<AnalyticsDashboard> {
    const params = new HttpParams().set('days', days);

    return this.http
      .get<ServiceResponse<AnalyticsDashboard>>(this.base + '/dashboard', { params })
      .pipe(map((response) => response.data));
  }
}
