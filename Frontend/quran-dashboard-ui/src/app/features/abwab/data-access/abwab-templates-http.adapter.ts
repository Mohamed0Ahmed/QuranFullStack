import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  AddDoorTemplateRequest,
  AddTemplateNodeAliasRequest,
  AddTemplateNodeRequest,
  ApplyDoorTemplateRequest,
  DeleteDoorTemplateRequest,
  DoorTemplateDetailDto,
  DoorTemplateListDto,
  EditDoorTemplateRequest,
  EditTemplateNodeAliasRequest,
  EditTemplateNodeRequest,
  RemoveTemplateNodeAliasRequest,
  RemoveTemplateNodeRequest,
  ReorderTemplateNodesRequest,
  ReparentTemplateNodeRequest,
  RestoreDoorTemplateRequest,
  RestoreTemplateNodeAliasRequest,
  TemplateHistoryDto,
} from '../../../core/api/generated/models';
import {
  AbwabTemplatesPort,
  AddDoorTemplateResult,
  AddTemplateNodeAliasResult,
  AddTemplateNodeResult,
} from './abwab-templates.port';
import { unwrapAbwabResponse } from './abwab-response-unwrap';

@Injectable({ providedIn: 'root' })
export class AbwabTemplatesHttpAdapter implements AbwabTemplatesPort {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/abwab/templates`;

  getTemplates(includeDeleted = false): Promise<DoorTemplateListDto> {
    return unwrapAbwabResponse(
      firstValueFrom(this.http.get<ApiResponse<DoorTemplateListDto>>(`${this.baseUrl}?includeDeleted=${includeDeleted}`)),
    );
  }

  getTemplate(doorTemplateId: string, includeDeleted = false): Promise<DoorTemplateDetailDto> {
    return unwrapAbwabResponse(
      firstValueFrom(
        this.http.get<ApiResponse<DoorTemplateDetailDto>>(`${this.baseUrl}/${doorTemplateId}?includeDeleted=${includeDeleted}`),
      ),
    );
  }

  getHistory(doorTemplateId: string): Promise<TemplateHistoryDto> {
    return unwrapAbwabResponse(
      firstValueFrom(this.http.get<ApiResponse<TemplateHistoryDto>>(`${this.baseUrl}/${doorTemplateId}/history`)),
    );
  }

  addTemplate(request: AddDoorTemplateRequest): Promise<AddDoorTemplateResult> {
    return unwrapAbwabResponse(firstValueFrom(this.http.post<ApiResponse<AddDoorTemplateResult>>(this.baseUrl, request)));
  }

  async editTemplate(doorTemplateId: string, request: EditDoorTemplateRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.put<ApiResponse<void>>(`${this.baseUrl}/${doorTemplateId}`, request)));
  }

  async deleteTemplate(doorTemplateId: string, request: DeleteDoorTemplateRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.delete<ApiResponse<void>>(`${this.baseUrl}/${doorTemplateId}`, { body: request })),
    );
  }

  async restoreTemplate(doorTemplateId: string, request: RestoreDoorTemplateRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/${doorTemplateId}/restore`, request)),
    );
  }

  addNode(doorTemplateId: string, request: AddTemplateNodeRequest): Promise<AddTemplateNodeResult> {
    return unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<AddTemplateNodeResult>>(`${this.baseUrl}/${doorTemplateId}/nodes`, request)),
    );
  }

  async editNode(templateNodeId: string, request: EditTemplateNodeRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.put<ApiResponse<void>>(`${this.baseUrl}/nodes/${templateNodeId}`, request)));
  }

  async reparentNode(templateNodeId: string, request: ReparentTemplateNodeRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/nodes/${templateNodeId}/reparent`, request)),
    );
  }

  async reorderNodes(doorTemplateId: string, request: ReorderTemplateNodesRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/${doorTemplateId}/nodes/reorder`, request)),
    );
  }

  async removeNode(templateNodeId: string, request: RemoveTemplateNodeRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.delete<ApiResponse<void>>(`${this.baseUrl}/nodes/${templateNodeId}`, { body: request })),
    );
  }

  addNodeAlias(templateNodeId: string, request: AddTemplateNodeAliasRequest): Promise<AddTemplateNodeAliasResult> {
    return unwrapAbwabResponse(
      firstValueFrom(
        this.http.post<ApiResponse<AddTemplateNodeAliasResult>>(`${this.baseUrl}/nodes/${templateNodeId}/aliases`, request),
      ),
    );
  }

  async editNodeAlias(aliasId: string, request: EditTemplateNodeAliasRequest): Promise<void> {
    await unwrapAbwabResponse(firstValueFrom(this.http.put<ApiResponse<void>>(`${this.baseUrl}/aliases/${aliasId}`, request)));
  }

  async removeNodeAlias(aliasId: string, request: RemoveTemplateNodeAliasRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.delete<ApiResponse<void>>(`${this.baseUrl}/aliases/${aliasId}`, { body: request })),
    );
  }

  async restoreNodeAlias(aliasId: string, request: RestoreTemplateNodeAliasRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/aliases/${aliasId}/restore`, request)),
    );
  }

  async applyTemplate(doorTemplateId: string, request: ApplyDoorTemplateRequest): Promise<void> {
    await unwrapAbwabResponse(
      firstValueFrom(this.http.post<ApiResponse<void>>(`${this.baseUrl}/${doorTemplateId}/apply`, request)),
    );
  }
}
