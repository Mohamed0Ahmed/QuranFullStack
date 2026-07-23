import { InjectionToken } from '@angular/core';

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

export interface AddCategoryResult {
  readonly categoryId: string;
}

export interface AddSectionResult {
  readonly sectionId: string;
}

export interface AddCategoryAliasResult {
  readonly aliasId: string;
}

export interface SubtreeDeleteResult {
  readonly deletionOperationId: string;
}

// The ONE versioned core contract §11/§18.3 step 4 requires: both the in-memory mock (T067) and the
// HTTP adapter (T068) implement this interface, so a parity suite (T063) can run the identical
// operation against each and assert identical outcomes. Every read carries TimelineGeneration/
// TreeRevision from the SERVER; no method here may synthesize one — see abwab-core.mock.ts and
// abwab-http.adapter.ts for the "neither fabricates TimelineGeneration" invariant.
export interface AbwabCorePort {
  getTreeSnapshot(): Promise<AbwabTreeSnapshotDto>;
  search(query: string): Promise<CategorySearchResultDto>;
  getProtectionProfile(categoryId: string): Promise<CategoryProtectionProfileDto>;

  addSection(request: AddSectionRequest): Promise<AddSectionResult>;
  editSection(sectionId: string, request: EditSectionRequest): Promise<void>;
  reorderSections(request: ReorderSectionsRequest): Promise<void>;
  deleteSection(sectionId: string, request: DeleteSectionRequest): Promise<void>;

  addCategory(request: AddCategoryRequest): Promise<AddCategoryResult>;
  editCategory(categoryId: string, request: EditCategoryRequest): Promise<void>;
  moveCategories(request: MoveCategoriesRequest): Promise<void>;
  reorderCategories(request: ReorderCategoriesRequest): Promise<void>;
  subtreeDeleteCategory(categoryId: string, request: SubtreeDeleteRequest): Promise<SubtreeDeleteResult>;
  operationRestoreCategory(deletionOperationId: string, request: OperationRestoreRequest): Promise<void>;

  addCategoryAlias(categoryId: string, request: AddCategoryAliasRequest): Promise<AddCategoryAliasResult>;
  editCategoryAlias(aliasId: string, request: EditCategoryAliasRequest): Promise<void>;
  removeCategoryAlias(aliasId: string, request: RemoveCategoryAliasRequest): Promise<void>;

  applyManualProtection(
    categoryId: string,
    protectionType: ManualProtectionType,
    request: ApplyManualProtectionRequest,
  ): Promise<void>;
  liftManualProtection(
    categoryId: string,
    protectionType: ManualProtectionType,
    request: LiftManualProtectionRequest,
  ): Promise<void>;
  applyFullProtectionPreset(categoryId: string, request: ApplyFullProtectionPresetRequest): Promise<void>;
}

export const ABWAB_CORE_PORT = new InjectionToken<AbwabCorePort>('ABWAB_CORE_PORT');
