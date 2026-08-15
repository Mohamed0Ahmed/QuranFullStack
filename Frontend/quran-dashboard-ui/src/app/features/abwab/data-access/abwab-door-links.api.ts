import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { DoorLinkMutationDto } from '../../../core/api/generated/models/door-link-mutation-dto';
import { DoorLinkSnapshotDto } from '../../../core/api/generated/models/door-link-snapshot-dto';
import { DeleteAbwabDoorLinksBody } from '../../../core/api/generated/models/delete-abwab-door-links-body';
import { ReplaceAbwabDoorLinkWordsBody } from '../../../core/api/generated/models/replace-abwab-door-link-words-body';
import { ApiResponse } from '../../../core/data-access/api-response.model';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/abwab/doors`;

  getSnapshot(doorId: number): Observable<ApiResponse<DoorLinkSnapshotDto>> {
    return this.http.get<ApiResponse<DoorLinkSnapshotDto>>(`${this.base}/${doorId}/links/snapshot`);
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
}
