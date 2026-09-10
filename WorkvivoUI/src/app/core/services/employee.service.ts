import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ServiceResponse } from '../models/service-response';
import {
  DirectoryFilters,
  EmployeeListItem,
  EmployeeProfile,
  OrganizationLookups,
  PagedResult,
} from '../models/employee';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api';

  /**
   * Filter lists for the directory.
   *
   * Shared and replayed: departments and offices change a few times a year, and every
   * component that renders a filter bar would otherwise re-fetch the same few hundred
   * rows. The server caches it too - this just avoids the round trip.
   */
  private lookups?: Observable<OrganizationLookups>;

  getDirectory(filters: DirectoryFilters): Observable<PagedResult<EmployeeListItem>> {
    let params = new HttpParams();

    // Only set parameters that have a value: an empty search term sent as `search=`
    // would be a filter that matches everything and defeats the index.
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return this.http
      .get<ServiceResponse<PagedResult<EmployeeListItem>>>(this.base + '/employees', { params })
      .pipe(map((response) => response.data));
  }

  getProfile(employeeId?: string): Observable<EmployeeProfile> {
    const url = employeeId ? `${this.base}/employees/${employeeId}` : `${this.base}/employees/me`;

    return this.http
      .get<ServiceResponse<EmployeeProfile>>(url)
      .pipe(map((response) => response.data));
  }

  getLookups(): Observable<OrganizationLookups> {
    this.lookups ??= this.http
      .get<ServiceResponse<OrganizationLookups>>(this.base + '/organization/lookups')
      .pipe(
        map((response) => response.data),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.lookups;
  }

  setFollowing(employeeId: string, follow: boolean): Observable<boolean> {
    const url = `${this.base}/employees/${employeeId}/follow`;

    const request = follow
      ? this.http.post<ServiceResponse<boolean>>(url, {})
      : this.http.delete<ServiceResponse<boolean>>(url);

    return request.pipe(map((response) => response.data));
  }
}
