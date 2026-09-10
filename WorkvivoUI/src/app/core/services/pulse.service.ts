import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Poll, SurveyAnswerInput, SurveyDetail, SurveySummary } from '../models/pulse';
import { ServiceResponse } from '../models/service-response';

/**
 * Polls and surveys.
 *
 * One service for both because they are one screen from the employee's side, even
 * though they are separate products underneath.
 */
@Injectable({ providedIn: 'root' })
export class PulseService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api';

  polls(includeClosed = false): Observable<Poll[]> {
    const params = new HttpParams().set('includeClosed', includeClosed);

    return this.http
      .get<ServiceResponse<Poll[]>>(this.base + '/polls', { params })
      .pipe(map((response) => response.data));
  }

  /** Returns the poll as the caller may now see it - including the tallies they just earned. */
  vote(pollId: string, optionIds: string[]): Observable<Poll> {
    return this.http
      .post<ServiceResponse<Poll>>(`${this.base}/polls/${pollId}/votes`, { optionIds })
      .pipe(map((response) => response.data));
  }

  surveys(includeClosed = false): Observable<SurveySummary[]> {
    const params = new HttpParams().set('includeClosed', includeClosed);

    return this.http
      .get<ServiceResponse<SurveySummary[]>>(this.base + '/surveys', { params })
      .pipe(map((response) => response.data));
  }

  survey(surveyId: string): Observable<SurveyDetail> {
    return this.http
      .get<ServiceResponse<SurveyDetail>>(`${this.base}/surveys/${surveyId}`)
      .pipe(map((response) => response.data));
  }

  respond(surveyId: string, answers: SurveyAnswerInput[]): Observable<string> {
    return this.http
      .post<ServiceResponse<string>>(`${this.base}/surveys/${surveyId}/responses`, { answers })
      .pipe(map((response) => response.data));
  }
}
