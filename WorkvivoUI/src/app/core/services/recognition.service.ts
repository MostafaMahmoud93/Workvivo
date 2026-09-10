import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CursorPage } from '../models/feed';
import {
  Leaderboard,
  LeaderboardPeriod,
  Recognition,
  RecognitionType,
  RecognitionVisibility,
} from '../models/recognition';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class RecognitionService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/recognition';

  types(): Observable<RecognitionType[]> {
    return this.http
      .get<ServiceResponse<RecognitionType[]>>(this.base + '/types')
      .pipe(map((response) => response.data));
  }

  wall(cursor: string | null, recipientEmployeeId?: string): Observable<CursorPage<Recognition>> {
    let params = new HttpParams().set('pageSize', 20);

    if (cursor) {
      params = params.set('cursor', cursor);
    }

    if (recipientEmployeeId) {
      params = params.set('recipientEmployeeId', recipientEmployeeId);
    }

    return this.http
      .get<ServiceResponse<CursorPage<Recognition>>>(this.base, { params })
      .pipe(map((response) => response.data));
  }

  give(
    recipientEmployeeId: string,
    recognitionTypeId: string,
    message: string,
    visibility: RecognitionVisibility,
  ): Observable<string> {
    return this.http
      .post<ServiceResponse<string>>(this.base, {
        recipientEmployeeId,
        recognitionTypeId,
        message,
        visibility,
      })
      .pipe(map((response) => response.data));
  }

  leaderboard(period: LeaderboardPeriod, top = 10): Observable<Leaderboard> {
    const params = new HttpParams().set('period', period).set('top', top);

    return this.http
      .get<ServiceResponse<Leaderboard>>(this.base + '/leaderboard', { params })
      .pipe(map((response) => response.data));
  }
}
