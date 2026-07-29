import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateNodeDto } from '../../../core/api/generated/models/abwab-template-node-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { CreateTemplateBody } from '../../../core/api/generated/models/create-template-body';
import { ApplyTemplateBody } from '../../../core/api/generated/models/apply-template-body';
import { AddTemplateNodeBody } from '../../../core/api/generated/models/add-template-node-body';
import { EditTemplateNodeBody } from '../../../core/api/generated/models/edit-template-node-body';
import { ReorderTemplateNodeBody } from '../../../core/api/generated/models/reorder-template-node-body';

/**
 * The nine templates endpoints. Its own data-access file rather than more methods on
 * `abwab.api.ts` (already fifteen): a separate route family, and nine of them.
 */
@Injectable({ providedIn: 'root' })
export class AbwabTemplatesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/abwab`;

  getTemplates(): Observable<ApiResponse<AbwabTemplateSummaryDto[]>> {
    return this.http.get<ApiResponse<AbwabTemplateSummaryDto[]>>(`${this.base}/templates`);
  }

  getTemplate(templateId: number): Observable<ApiResponse<AbwabTemplateDto>> {
    return this.http.get<ApiResponse<AbwabTemplateDto>>(`${this.base}/templates/${templateId}`);
  }

  createTemplate(body: CreateTemplateBody): Observable<ApiResponse<AbwabTemplateDto>> {
    return this.http.post<ApiResponse<AbwabTemplateDto>>(`${this.base}/templates`, body);
  }

  // 204 No Content on success, so HttpClient yields `null` rather than an envelope.
  deleteTemplate(templateId: number): Observable<ApiResponse<unknown> | null> {
    return this.http.delete<ApiResponse<unknown> | null>(`${this.base}/templates/${templateId}`);
  }

  applyTemplate(templateId: number, body: ApplyTemplateBody): Observable<ApiResponse<AbwabDoorDto[]>> {
    return this.http.post<ApiResponse<AbwabDoorDto[]>>(`${this.base}/templates/${templateId}/apply`, body);
  }

  addNode(templateId: number, body: AddTemplateNodeBody): Observable<ApiResponse<AbwabTemplateNodeDto>> {
    return this.http.post<ApiResponse<AbwabTemplateNodeDto>>(`${this.base}/templates/${templateId}/nodes`, body);
  }

  editNode(nodeId: number, body: EditTemplateNodeBody): Observable<ApiResponse<AbwabTemplateNodeDto>> {
    return this.http.put<ApiResponse<AbwabTemplateNodeDto>>(`${this.base}/template-nodes/${nodeId}`, body);
  }

  reorderNode(nodeId: number, body: ReorderTemplateNodeBody): Observable<ApiResponse<AbwabTemplateNodeDto>> {
    return this.http.post<ApiResponse<AbwabTemplateNodeDto>>(`${this.base}/template-nodes/${nodeId}/order`, body);
  }

  // 204 No Content on success, so HttpClient yields `null` rather than an envelope.
  deleteNode(nodeId: number): Observable<ApiResponse<unknown> | null> {
    return this.http.delete<ApiResponse<unknown> | null>(`${this.base}/template-nodes/${nodeId}`);
  }
}
