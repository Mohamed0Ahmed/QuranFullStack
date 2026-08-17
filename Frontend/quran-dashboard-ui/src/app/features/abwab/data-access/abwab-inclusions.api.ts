import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabDoorInclusionAddResultDto } from '../../../core/api/generated/models/abwab-door-inclusion-add-result-dto';
import { AbwabDoorInclusionTopologyDto } from '../../../core/api/generated/models/abwab-door-inclusion-topology-dto';
import { AddAbwabDoorInclusionsBody } from '../../../core/api/generated/models/add-abwab-door-inclusions-body';

@Injectable({ providedIn: 'root' })
export class AbwabInclusionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/abwab/doors`;

  getTopology(doorId: number): Observable<ApiResponse<AbwabDoorInclusionTopologyDto>> {
    return this.http.get<ApiResponse<AbwabDoorInclusionTopologyDto>>(
      `${this.base}/${doorId}/inclusions`,
    );
  }

  addSources(
    targetDoorId: number,
    body: AddAbwabDoorInclusionsBody,
  ): Observable<ApiResponse<AbwabDoorInclusionAddResultDto>> {
    return this.http.post<ApiResponse<AbwabDoorInclusionAddResultDto>>(
      `${this.base}/${targetDoorId}/inclusions`,
      body,
    );
  }
}
