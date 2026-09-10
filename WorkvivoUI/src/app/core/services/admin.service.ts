import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuditEntry, Role } from '../models/admin';
import { PagedResult } from '../models/paging';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/admin';

  roles(): Observable<Role[]> {
    return this.http
      .get<ServiceResponse<Role[]>>(this.base + '/roles')
      .pipe(map((response) => response.data));
  }

  auditLog(pageNumber: number, entityName: string): Observable<PagedResult<AuditEntry>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', 25);

    if (entityName.trim()) {
      params = params.set('entityName', entityName.trim());
    }

    return this.http
      .get<ServiceResponse<PagedResult<AuditEntry>>>(this.base + '/audit', { params })
      .pipe(map((response) => response.data));
  }
}
