import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { DoorLinkAyahsPageDto } from '../../../core/api/generated/models/door-link-ayahs-page-dto';
import { DoorLinkMutationDto } from '../../../core/api/generated/models/door-link-mutation-dto';
import { DoorLinkRecordsPageDto } from '../../../core/api/generated/models/door-link-records-page-dto';
import { DoorLinkSnapshotDto } from '../../../core/api/generated/models/door-link-snapshot-dto';
import { DeleteAbwabDoorLinksBody } from '../../../core/api/generated/models/delete-abwab-door-links-body';
import { ReplaceAbwabDoorLinkWordsBody } from '../../../core/api/generated/models/replace-abwab-door-link-words-body';
import { ApiResponse } from '../../../core/data-access/api-response.model';

export interface AbwabDoorLinkRecordsRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly expectedDoorVersion: number | null;
}

export interface AbwabDoorLinkAyahsRequest extends AbwabDoorLinkRecordsRequest {
  readonly expectedLinkingDataRevision: number | null;
}

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/abwab/doors`;

  getSnapshot(doorId: number): Observable<ApiResponse<DoorLinkSnapshotDto>> {
    return this.http.get<ApiResponse<DoorLinkSnapshotDto>>(`${this.base}/${doorId}/links/snapshot`);
  }

  getRecords(
    doorId: number,
    request: AbwabDoorLinkRecordsRequest,
  ): Observable<ApiResponse<DoorLinkRecordsPageDto>> {
    return this.http.get<ApiResponse<DoorLinkRecordsPageDto>>(`${this.base}/${doorId}/links`, {
      params: this.pageParams(request),
    });
  }

  getAyahs(
    doorId: number,
    unitId: number,
    request: AbwabDoorLinkAyahsRequest,
  ): Observable<ApiResponse<DoorLinkAyahsPageDto>> {
    let params = this.pageParams(request);
    if (request.expectedLinkingDataRevision !== null) {
      params = params.set('expectedLinkingDataRevision', request.expectedLinkingDataRevision);
    }
    return this.http.get<ApiResponse<DoorLinkAyahsPageDto>>(
      `${this.base}/${doorId}/links/${unitId}/ayahs`,
      { params },
    );
  }

  replaceWords(
    doorId: number,
    unitId: number,
    body: ReplaceAbwabDoorLinkWordsBody,
  ): Observable<ApiResponse<DoorLinkMutationDto>> {
    return this.http.patch<ApiResponse<DoorLinkMutationDto>>(
      `${this.base}/${doorId}/links/${unitId}/words`,
      body,
    );
  }

  deleteLinks(
    doorId: number,
    body: DeleteAbwabDoorLinksBody,
  ): Observable<ApiResponse<DoorLinkMutationDto>> {
    return this.http.post<ApiResponse<DoorLinkMutationDto>>(`${this.base}/${doorId}/links/bulk-delete`, body);
  }

  private pageParams(request: AbwabDoorLinkRecordsRequest): HttpParams {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    if (request.expectedDoorVersion !== null) {
      params = params.set('expectedDoorVersion', request.expectedDoorVersion);
    }
    return params;
  }
}
