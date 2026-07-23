import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  AbwabTreeSnapshotDto,
  AddCategoryAliasRequest,
  AddCategoryRequest,
  AddSectionRequest,
  ApplyFullProtectionPresetRequest,
  ApplyManualProtectionRequest,
  CategoryProtectionProfileDto,
  CategorySearchResultDto,
  DeleteSectionRequest,
  EditCategoryAliasRequest,
  EditCategoryRequest,
  EditSectionRequest,
  LiftManualProtectionRequest,
  ManualProtectionType,
  MoveCategoriesRequest,
  OperationRestoreRequest,
  RemoveCategoryAliasRequest,
  ReorderCategoriesRequest,
  ReorderSectionsRequest,
  SubtreeDeleteRequest,
} from '../../../core/api/generated/models';
import { AbwabConflictError, isAbwabConflictCode } from './abwab-conflict';
import {
  AbwabCorePort,
  AddCategoryAliasResult,
  AddCategoryResult,
  AddSectionResult,
  SubtreeDeleteResult,
} from './abwab-core.port';

const UNEXPECTED_ERROR = 'حدث خطأ غير متوقع.';

@Injectable({ providedIn: 'root' })
export class AbwabHttpAdapter implements AbwabCorePort {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/abwab`;

  async getTreeSnapshot(): Promise<AbwabTreeSnapshotDto> {
    return this.get<AbwabTreeSnapshotDto>('/tree');
  }

  async search(query: string): Promise<CategorySearchResultDto> {
    return this.get<CategorySearchResultDto>(`/tree/search?query=${encodeURIComponent(query)}`);
  }

  async getProtectionProfile(categoryId: string): Promise<CategoryProtectionProfileDto> {
    return this.get<CategoryProtectionProfileDto>(`/protection/${categoryId}`);
  }

  async addSection(request: AddSectionRequest): Promise<AddSectionResult> {
    return this.post<AddSectionResult>('/sections', request);
  }

  async editSection(sectionId: string, request: EditSectionRequest): Promise<void> {
    await this.put(`/sections/${sectionId}`, request);
  }

  async reorderSections(request: ReorderSectionsRequest): Promise<void> {
    await this.post('/sections/reorder', request);
  }

  async deleteSection(sectionId: string, request: DeleteSectionRequest): Promise<void> {
    await this.delete(`/sections/${sectionId}`, request);
  }

  async addCategory(request: AddCategoryRequest): Promise<AddCategoryResult> {
    return this.post<AddCategoryResult>('/categories', request);
  }

  async editCategory(categoryId: string, request: EditCategoryRequest): Promise<void> {
    await this.put(`/categories/${categoryId}`, request);
  }

  async moveCategories(request: MoveCategoriesRequest): Promise<void> {
    await this.post('/categories/move', request);
  }

  async reorderCategories(request: ReorderCategoriesRequest): Promise<void> {
    await this.post('/categories/reorder', request);
  }

  async subtreeDeleteCategory(categoryId: string, request: SubtreeDeleteRequest): Promise<SubtreeDeleteResult> {
    return this.post<SubtreeDeleteResult>(`/categories/${categoryId}/subtree-delete`, request);
  }

  async operationRestoreCategory(deletionOperationId: string, request: OperationRestoreRequest): Promise<void> {
    await this.post(`/categories/operation-restore/${deletionOperationId}`, request);
  }

  async addCategoryAlias(categoryId: string, request: AddCategoryAliasRequest): Promise<AddCategoryAliasResult> {
    return this.post<AddCategoryAliasResult>(`/categories/${categoryId}/aliases`, request);
  }

  async editCategoryAlias(aliasId: string, request: EditCategoryAliasRequest): Promise<void> {
    await this.put(`/categories/aliases/${aliasId}`, request);
  }

  async removeCategoryAlias(aliasId: string, request: RemoveCategoryAliasRequest): Promise<void> {
    await this.delete(`/categories/aliases/${aliasId}`, request);
  }

  async applyManualProtection(categoryId: string, protectionType: ManualProtectionType, request: ApplyManualProtectionRequest): Promise<void> {
    await this.post(`/protection/${categoryId}/${protectionType}/apply`, request);
  }

  async liftManualProtection(categoryId: string, protectionType: ManualProtectionType, request: LiftManualProtectionRequest): Promise<void> {
    await this.post(`/protection/${categoryId}/${protectionType}/lift`, request);
  }

  async applyFullProtectionPreset(categoryId: string, request: ApplyFullProtectionPresetRequest): Promise<void> {
    await this.post(`/protection/${categoryId}/full-preset`, request);
  }

  private async get<T>(path: string): Promise<T> {
    return this.unwrap(firstValueFrom(this.http.get<ApiResponse<T>>(`${this.baseUrl}${path}`)));
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    return this.unwrap(firstValueFrom(this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, body)));
  }

  private async put<T>(path: string, body: unknown): Promise<T> {
    return this.unwrap(firstValueFrom(this.http.put<ApiResponse<T>>(`${this.baseUrl}${path}`, body)));
  }

  private async delete<T>(path: string, body: unknown): Promise<T> {
    return this.unwrap(firstValueFrom(this.http.delete<ApiResponse<T>>(`${this.baseUrl}${path}`, { body })));
  }

  private async unwrap<T>(request: Promise<ApiResponse<T>>): Promise<T> {
    let response: ApiResponse<T>;
    try {
      response = await request;
    } catch (error) {
      throw this.toDomainError(error);
    }
    if (!response.isSuccess) {
      throw new Error(response.message ?? UNEXPECTED_ERROR);
    }
    return response.data as T;
  }

  private toDomainError(error: unknown): Error {
    if (error instanceof HttpErrorResponse && error.status === 409) {
      const code = this.readFirstErrorCode(error.error);
      if (code && isAbwabConflictCode(code)) {
        return new AbwabConflictError(code);
      }
    }
    if (error instanceof HttpErrorResponse) {
      const body = error.error as Partial<ApiResponse<unknown>> | undefined;
      return new Error(body?.message ?? UNEXPECTED_ERROR);
    }
    return error instanceof Error ? error : new Error(UNEXPECTED_ERROR);
  }

  private readFirstErrorCode(body: unknown): string | null {
    if (typeof body !== 'object' || body === null) {
      return null;
    }
    const errors = (body as Partial<ApiResponse<unknown>>).errors;
    return Array.isArray(errors) && typeof errors[0] === 'string' ? errors[0] : null;
  }
}
