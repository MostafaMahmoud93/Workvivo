import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ServiceResponse } from '../models/service-response';
import { Comment, CursorPage, FeedItem, ReactionType, REACTION_TYPES } from '../models/feed';

@Injectable({ providedIn: 'root' })
export class FeedService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl + '/api/posts';

  getFeed(cursor: string | null, pageSize = 10): Observable<CursorPage<FeedItem>> {
    let params = new HttpParams().set('pageSize', pageSize);

    if (cursor) {
      params = params.set('cursor', cursor);
    }

    return this.http
      .get<ServiceResponse<CursorPage<FeedItem>>>(this.base + '/feed', { params })
      .pipe(map((response) => response.data));
  }

  getComments(postId: string, cursor: string | null): Observable<CursorPage<Comment>> {
    let params = new HttpParams().set('pageSize', 20);

    if (cursor) {
      params = params.set('cursor', cursor);
    }

    return this.http
      .get<ServiceResponse<CursorPage<Comment>>>(`${this.base}/${postId}/comments`, { params })
      .pipe(map((response) => response.data));
  }

  addComment(postId: string, contentHtml: string, parentCommentId?: string): Observable<string> {
    return this.http
      .post<ServiceResponse<string>>(`${this.base}/${postId}/comments`, {
        parentCommentId: parentCommentId ?? null,
        contentHtml,
        mentionedEmployeeIds: [],
      })
      .pipe(map((response) => response.data));
  }

  /** Null clears the caller's reaction. Returns the new total. */
  react(postId: string, reaction: ReactionType | null): Observable<number> {
    return this.http
      .put<ServiceResponse<number>>(`${this.base}/${postId}/reactions`, {
        // The API takes the enum's numeric value; the client works in names.
        reaction: reaction === null ? null : REACTION_TYPES.indexOf(reaction),
      })
      .pipe(map((response) => response.data));
  }

  /**
   * Reports posts the reader has actually seen.
   *
   * One request per batch, not per post - this is the highest-volume write in the
   * product and a request per row would multiply every scroll by twenty.
   */
  recordViews(postIds: string[]): Observable<number> {
    return this.http
      .post<ServiceResponse<number>>(this.base + '/views', { postIds })
      .pipe(map((response) => response.data));
  }

  createPost(contentHtml: string, title: string | null): Observable<string> {
    return this.http
      .post<ServiceResponse<string>>(this.base, {
        postType: 0,
        titleEn: title,
        titleAr: title,
        contentHtml,
        communityId: null,
        isOfficial: false,
        commentsEnabled: true,
        scheduledPublishDate: null,
        publishNow: true,
        // Everyone, for now. The composer gains an audience picker with the
        // targeting UI.
        audiences: [{ audienceType: 0, targetId: null }],
        mentionedEmployeeIds: [],
      })
      .pipe(map((response) => response.data));
  }
}
