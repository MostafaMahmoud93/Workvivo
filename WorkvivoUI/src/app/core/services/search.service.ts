import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SearchResults } from '../models/search';
import { ServiceResponse } from '../models/service-response';

/** Matches the server's floor; below it a term matches most of the company. */
export const MIN_SEARCH_LENGTH = 2;

const EMPTY: SearchResults = {
  term: '',
  employees: [],
  posts: [],
  communities: [],
  documents: [],
  events: [],
  totalShown: 0,
};

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/search';

  search(term: string, take = 5): Observable<SearchResults> {
    if (term.trim().length < MIN_SEARCH_LENGTH) {
      // The server would return nothing anyway; not asking saves a round trip on
      // every keystroke of the first two characters.
      return of({ ...EMPTY, term });
    }

    const params = new HttpParams().set('term', term.trim()).set('take', take);

    return this.http
      .get<ServiceResponse<SearchResults>>(this.base, { params })
      .pipe(map((response) => response.data));
  }
}
