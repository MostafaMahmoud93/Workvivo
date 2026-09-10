import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CalendarEvent, EventResponse } from '../models/event';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/events';

  list(includePast = false, days = 60): Observable<CalendarEvent[]> {
    const params = new HttpParams().set('includePast', includePast).set('days', days);

    return this.http
      .get<ServiceResponse<CalendarEvent[]>>(this.base, { params })
      .pipe(map((response) => response.data));
  }

  /** Returns the new attendee count. */
  rsvp(eventId: string, response: EventResponse): Observable<number> {
    return this.http
      .post<ServiceResponse<number>>(`${this.base}/${eventId}/rsvp`, { response })
      .pipe(map((result) => result.data));
  }
}
