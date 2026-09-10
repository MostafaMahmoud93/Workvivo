import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CommunityDetail,
  CommunityInvitation,
  CommunityMember,
  CommunityMemberRole,
  CommunityPrivacy,
  CommunitySummary,
  MembershipDecision,
  MembershipStatus,
} from '../models/community';
import { PagedResult } from '../models/paging';
import { ServiceResponse } from '../models/service-response';

@Injectable({ providedIn: 'root' })
export class CommunityService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/communities';

  list(
    scope: 'Discover' | 'Mine',
    search: string,
    pageNumber = 1,
  ): Observable<PagedResult<CommunitySummary>> {
    let params = new HttpParams().set('scope', scope).set('pageNumber', pageNumber).set('pageSize', 24);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http
      .get<ServiceResponse<PagedResult<CommunitySummary>>>(this.base, { params })
      .pipe(map((response) => response.data));
  }

  detail(communityId: string): Observable<CommunityDetail> {
    return this.http
      .get<ServiceResponse<CommunityDetail>>(`${this.base}/${communityId}`)
      .pipe(map((response) => response.data));
  }

  members(communityId: string, pendingOnly: boolean): Observable<PagedResult<CommunityMember>> {
    const params = new HttpParams().set('pendingOnly', pendingOnly).set('pageSize', 50);

    return this.http
      .get<ServiceResponse<PagedResult<CommunityMember>>>(`${this.base}/${communityId}/members`, { params })
      .pipe(map((response) => response.data));
  }

  /** Returns the resulting membership status - joined, or waiting for approval. */
  join(communityId: string): Observable<MembershipStatus> {
    return this.http
      .post<ServiceResponse<MembershipStatus>>(`${this.base}/${communityId}/membership`, {})
      .pipe(map((response) => response.data));
  }

  leave(communityId: string): Observable<void> {
    return this.http
      .delete<ServiceResponse<string>>(`${this.base}/${communityId}/membership`)
      .pipe(map(() => undefined));
  }

  review(communityId: string, employeeId: string, decision: MembershipDecision): Observable<void> {
    return this.http
      .post<ServiceResponse<string>>(
        `${this.base}/${communityId}/members/${employeeId}/review`,
        { decision },
      )
      .pipe(map(() => undefined));
  }

  setRole(communityId: string, employeeId: string, role: CommunityMemberRole): Observable<void> {
    return this.http
      .put<ServiceResponse<string>>(`${this.base}/${communityId}/members/${employeeId}/role`, { role })
      .pipe(map(() => undefined));
  }

  save(
    id: string | null,
    nameAr: string,
    nameEn: string | null,
    descriptionAr: string | null,
    descriptionEn: string | null,
    privacy: CommunityPrivacy,
  ): Observable<string> {
    return this.http
      .post<ServiceResponse<string>>(this.base, {
        id,
        nameAr,
        nameEn,
        descriptionAr,
        descriptionEn,
        privacy,
      })
      .pipe(map((response) => response.data));
  }

  myInvitations(): Observable<CommunityInvitation[]> {
    return this.http
      .get<ServiceResponse<CommunityInvitation[]>>(this.base + '/invitations/mine')
      .pipe(map((response) => response.data));
  }

  respondToInvitation(invitationId: string, accept: boolean): Observable<void> {
    return this.http
      .post<ServiceResponse<string>>(`${this.base}/invitations/${invitationId}`, { accept })
      .pipe(map(() => undefined));
  }
}
