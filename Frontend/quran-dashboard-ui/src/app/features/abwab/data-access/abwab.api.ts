import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { AbwabRestoredDoorDto } from '../../../core/api/generated/models/abwab-restored-door-dto';
import { CreateSectionCommand } from '../../../core/api/generated/models/create-section-command';
import { RenameSectionBody } from '../../../core/api/generated/models/rename-section-body';
import { ReorderSectionBody } from '../../../core/api/generated/models/reorder-section-body';
import { CreateDoorCommand } from '../../../core/api/generated/models/create-door-command';
import { EditDoorBody } from '../../../core/api/generated/models/edit-door-body';
import { MoveDoorBody } from '../../../core/api/generated/models/move-door-body';
import { ReorderDoorBody } from '../../../core/api/generated/models/reorder-door-body';
import { BulkMoveDoorsCommand } from '../../../core/api/generated/models/bulk-move-doors-command';
import { BulkArchiveDoorsCommand } from '../../../core/api/generated/models/bulk-archive-doors-command';
import { DeleteDoorBody } from '../../../core/api/generated/models/delete-door-body';
import { RestoreDoorBody } from '../../../core/api/generated/models/restore-door-body';
import { AbwabDoorRelationDto } from '../../../core/api/generated/models/abwab-door-relation-dto';
import { AddDoorRelationsBody } from '../../../core/api/generated/models/add-door-relations-body';

/**
 * Wire body for door creation. `sectionId` is optional here (unlike the generated
 * `CreateDoorCommand`, where it is a required nullable key) because the backend derives
 * the section from `parentId` when set and 400s on a stated mismatch
 * (plan-slice-b.md §4, input 5) — so the key must be genuinely absent, not `null`, once a
 * parent is given.
 */
type CreateDoorWireBody = Omit<CreateDoorCommand, 'sectionId'> & { sectionId?: number | null };

function buildCreateDoorBody(command: CreateDoorCommand): CreateDoorWireBody {
  const { sectionId, ...rest } = command;
  return command.parentId != null ? rest : { ...rest, sectionId };
}

@Injectable({ providedIn: 'root' })
export class AbwabApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/abwab`;

  getTree(): Observable<ApiResponse<AbwabTreeDto>> {
    return this.http.get<ApiResponse<AbwabTreeDto>>(`${this.base}/tree`);
  }

  createSection(command: CreateSectionCommand): Observable<ApiResponse<AbwabSectionDto>> {
    return this.http.post<ApiResponse<AbwabSectionDto>>(`${this.base}/sections`, command);
  }

  renameSection(id: number, body: RenameSectionBody): Observable<ApiResponse<AbwabSectionDto>> {
    return this.http.put<ApiResponse<AbwabSectionDto>>(`${this.base}/sections/${id}`, body);
  }

  // 204 No Content on success, so HttpClient yields `null` rather than an envelope.
  deleteSection(id: number): Observable<ApiResponse<unknown> | null> {
    return this.http.delete<ApiResponse<unknown> | null>(`${this.base}/sections/${id}`);
  }

  reorderSection(id: number, body: ReorderSectionBody): Observable<ApiResponse<AbwabSectionDto>> {
    return this.http.post<ApiResponse<AbwabSectionDto>>(`${this.base}/sections/${id}/order`, body);
  }

  createDoor(command: CreateDoorCommand): Observable<ApiResponse<AbwabDoorDto>> {
    return this.http.post<ApiResponse<AbwabDoorDto>>(`${this.base}/doors`, buildCreateDoorBody(command));
  }

  updateDoor(id: number, body: EditDoorBody): Observable<ApiResponse<AbwabDoorDto>> {
    return this.http.put<ApiResponse<AbwabDoorDto>>(`${this.base}/doors/${id}`, body);
  }

  moveDoor(id: number, body: MoveDoorBody): Observable<ApiResponse<AbwabDoorDto>> {
    return this.http.post<ApiResponse<AbwabDoorDto>>(`${this.base}/doors/${id}/move`, body);
  }

  reorderDoor(id: number, body: ReorderDoorBody): Observable<ApiResponse<AbwabDoorDto>> {
    return this.http.post<ApiResponse<AbwabDoorDto>>(`${this.base}/doors/${id}/order`, body);
  }

  bulkMoveDoors(command: BulkMoveDoorsCommand): Observable<ApiResponse<AbwabDoorDto[]>> {
    return this.http.post<ApiResponse<AbwabDoorDto[]>>(`${this.base}/doors/bulk-move`, command);
  }

  bulkArchiveDoors(command: BulkArchiveDoorsCommand): Observable<ApiResponse<number[]>> {
    return this.http.post<ApiResponse<number[]>>(`${this.base}/doors/bulk-archive`, command);
  }

  // Archive is `DELETE` **with** a body — Angular's `HttpClient.delete` needs `{ body }`.
  // 204 No Content on success, so HttpClient yields `null` rather than an envelope.
  archiveDoor(id: number, body: DeleteDoorBody): Observable<ApiResponse<unknown> | null> {
    return this.http.delete<ApiResponse<unknown> | null>(`${this.base}/doors/${id}`, { body });
  }

  restoreDoor(id: number, body: RestoreDoorBody): Observable<ApiResponse<AbwabRestoredDoorDto>> {
    return this.http.post<ApiResponse<AbwabRestoredDoorDto>>(`${this.base}/doors/${id}/restore`, body);
  }

  getDoorRelations(doorId: number): Observable<ApiResponse<AbwabDoorRelationDto[]>> {
    return this.http.get<ApiResponse<AbwabDoorRelationDto[]>>(`${this.base}/doors/${doorId}/relations`);
  }

  addDoorRelations(doorId: number, body: AddDoorRelationsBody): Observable<ApiResponse<AbwabDoorRelationDto[]>> {
    return this.http.post<ApiResponse<AbwabDoorRelationDto[]>>(`${this.base}/doors/${doorId}/relations`, body);
  }

  // 204 No Content on success, so HttpClient yields `null` rather than an envelope.
  deleteRelation(relationId: number): Observable<ApiResponse<unknown> | null> {
    return this.http.delete<ApiResponse<unknown> | null>(`${this.base}/relations/${relationId}`);
  }
}
