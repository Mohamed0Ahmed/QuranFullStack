import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  AddRelationshipRequest,
  CategoryRelationshipListDto,
  DeleteRelationshipRequest,
  EditRelationshipRequest,
  RestoreRelationshipRequest,
} from '../../../core/api/generated/models';
import { AbwabRelationshipsPort, AddRelationshipResult } from './abwab-relationships.port';
import { unwrapAbwabResponse } from './abwab-response-unwrap';

@Injectable({ providedIn: 'root' })
export class AbwabRelationshipsHttpAdapter implements AbwabRelationshipsPort {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/abwab/relationships`;

  async getRelationships(categoryId: string, includeDeleted = false): Promise<CategoryRelationshipListDto> {
    return unwrapAbwabResponse(
      firstValueFrom(
        this.http.get<ApiResponse<CategoryRelationshipListDto>>(`${this.baseUrl}/${categoryId}?includeDeleted=${includeDeleted}`),
      ),
    );
  }

  async addRelationship(request: AddRelationshipRequest): Promise<AddRelationshipResult> {
    return unwrapAbwabResponse(firstValueFrom(this.http.post<ApiResponse<AddRelationshipResult>>(this.baseUrl, request)));
  }

  async editRelationship(relationshipId: string, request: EditRelationshipRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.put<ApiResponse<void>>(`${this.baseUrl}/${relationshipId}`, request)));
  }

  async deleteRelationship(relationshipId: string, request: DeleteRelationshipRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.delete<ApiResponse<void>>(`${this.baseUrl}/${relationshipId}`, { body: request })));
  }

  async restoreRelationship(relationshipId: string, request: RestoreRelationshipRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/${relationshipId}/restore`, request)));
  }
}
