import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { MushafDoorHighlightsResponse } from '../../../core/api/generated/models';
import { ApiResponse } from '../../../core/data-access/api-response.model';

@Injectable({ providedIn: 'root' })
export class MushafDoorHighlightsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getPageHighlights(
    pageNumber: number,
    doorIds: readonly number[],
  ): Observable<ApiResponse<MushafDoorHighlightsResponse>> {
    const params = doorIds.reduce(
      (current, doorId) => current.append('doorIds', doorId),
      new HttpParams(),
    );

    return this.http.get<ApiResponse<MushafDoorHighlightsResponse>>(
      `${this.baseUrl}/api/mushaf/pages/${pageNumber}/door-highlights`,
      { params },
    );
  }
}
