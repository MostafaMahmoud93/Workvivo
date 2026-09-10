import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DocumentCategory, DocumentSummary } from '../models/document';
import { PagedResult } from '../models/paging';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/documents';

  categories(): Observable<DocumentCategory[]> {
    return this.http
      .get<ServiceResponse<DocumentCategory[]>>(this.base + '/categories')
      .pipe(map((response) => response.data));
  }

  list(categoryId: string | null, search: string): Observable<PagedResult<DocumentSummary>> {
    let params = new HttpParams().set('pageSize', 50);

    if (categoryId) {
      params = params.set('categoryId', categoryId);
    }

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http
      .get<ServiceResponse<PagedResult<DocumentSummary>>>(this.base, { params })
      .pipe(map((response) => response.data));
  }

  /**
   * Fetches the bytes through the authorised endpoint and hands them to the browser.
   *
   * A plain anchor would not carry the bearer token - it lives in memory, not in a
   * cookie - so the request has to go through HttpClient and the resulting blob is
   * saved with a temporary object URL.
   */
  download(document_: DocumentSummary): Observable<Blob> {
    return this.http.get(`${this.base}/${document_.id}/content`, { responseType: 'blob' });
  }
}
