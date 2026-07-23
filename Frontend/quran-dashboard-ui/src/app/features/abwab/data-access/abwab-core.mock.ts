import {
  AbwabTreeSnapshotDto,
  AddCategoryAliasRequest,
  AddCategoryRequest,
  AddSectionRequest,
  ApplyFullProtectionPresetRequest,
  ApplyManualProtectionRequest,
  CategoryProtectionProfileDto,
  CategorySearchResultDto,
  CategorySnapshotDto,
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
  SectionSnapshotDto,
  SubtreeDeleteRequest,
} from '../../../core/api/generated/models';
import { addCategoryAlias, editCategoryAlias, removeCategoryAlias } from './abwab-mock-alias-ops';
import { addCategory, editCategory } from './abwab-mock-category-ops';
import {
  AddCategoryAliasResult,
  AddCategoryResult,
  AddSectionResult,
  AbwabCorePort,
  SubtreeDeleteResult,
} from './abwab-core.port';
import { operationRestoreCategory, subtreeDeleteCategory } from './abwab-mock-delete-restore-ops';
import { moveCategories, reorderCategories } from './abwab-mock-move-ops';
import { normalizeArabicNameForUi } from './abwab-mock-normalize';
import { applyFullProtectionPreset, applyManualProtection, liftManualProtection, resolveProtectionProfile } from './abwab-mock-protection-ops';
import { addSection, deleteSection, editSection, reorderSections } from './abwab-mock-section-ops';
import { MockClock, SYSTEM_MOCK_CLOCK } from './abwab-mock-shared';
import { AbwabMockState, createSeededMockState } from './abwab-mock-state';

let mockIdCounter = 0;
export function nextMockId(prefix: string): string {
  mockIdCounter += 1;
  return `${prefix}-${mockIdCounter}`;
}

export interface AbwabCoreMockOptions {
  readonly actorSubject?: string;
  readonly idFactory?: () => string;
  readonly clock?: MockClock;
}

// T067: in-memory implementation of the ONE versioned AbwabCorePort contract, standing in for the
// backend during frontend development/testing. It re-derives its own TreeRevision/TimelineGeneration
// internally and returns them ONLY from reads (getTreeSnapshot/search/getProtectionProfile) — mutation
// methods never attach a generation/revision to their result, matching the real HTTP surface exactly
// (see abwab-http.adapter.ts) so the parity suite (T063) holds.
//
// Not @Injectable(): it takes plain constructor options (actor/clock/id factory) rather than
// injected services, so callers construct it directly or via a DI `useFactory`.
export class AbwabCoreMock implements AbwabCorePort {
  private readonly actorSubject: string;
  private readonly idFactory: () => string;
  private readonly clock: MockClock;
  private readonly state: AbwabMockState;

  constructor(options: AbwabCoreMockOptions = {}) {
    this.actorSubject = options.actorSubject ?? 'mock-user';
    this.idFactory = options.idFactory ?? (() => nextMockId('id'));
    this.clock = options.clock ?? SYSTEM_MOCK_CLOCK;
    this.state = createSeededMockState(this.idFactory);
  }

  async getTreeSnapshot(): Promise<AbwabTreeSnapshotDto> {
    const sections = [...this.state.sections.values()]
      .filter((section) => !section.deleted)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(toSectionDto);

    const categories = [...this.state.categories.values()]
      .filter((category) => !category.deleted)
      .map((category) => this.toCategoryDto(category.categoryId));

    return {
      schemaVersion: 1,
      treeRevision: this.state.treeRevision,
      expectedTimelineGeneration: { generation: this.state.timelineGeneration },
      serverTimeUtc: this.clock.nowUtc(),
      sections,
      categories,
      allCategoriesProjection: categories,
    };
  }

  async search(query: string): Promise<CategorySearchResultDto> {
    const normalizedQuery = normalizeArabicNameForUi(query);
    const matches = [...this.state.categories.values()]
      .filter((category) => !category.deleted)
      .filter((category) => {
        if (normalizedQuery.length === 0) {
          return false;
        }
        if (category.normalizedName.includes(normalizedQuery)) {
          return true;
        }
        return [...this.state.aliases.values()].some(
          (alias) => !alias.deleted && alias.categoryId === category.categoryId && alias.normalizedValue.includes(normalizedQuery),
        );
      })
      .map((category) => this.toCategoryDto(category.categoryId));

    return { expectedTimelineGeneration: { generation: this.state.timelineGeneration }, matches };
  }

  async getProtectionProfile(categoryId: string): Promise<CategoryProtectionProfileDto> {
    return resolveProtectionProfile(this.state, categoryId, this.clock);
  }

  async addSection(request: AddSectionRequest): Promise<AddSectionResult> {
    return { sectionId: addSection(this.state, this.idFactory, request) };
  }

  async editSection(sectionId: string, request: EditSectionRequest): Promise<void> {
    editSection(this.state, sectionId, request);
  }

  async reorderSections(request: ReorderSectionsRequest): Promise<void> {
    reorderSections(this.state, request);
  }

  async deleteSection(sectionId: string, request: DeleteSectionRequest): Promise<void> {
    deleteSection(this.state, sectionId, request);
  }

  async addCategory(request: AddCategoryRequest): Promise<AddCategoryResult> {
    return { categoryId: addCategory(this.state, this.idFactory, request) };
  }

  async editCategory(categoryId: string, request: EditCategoryRequest): Promise<void> {
    editCategory(this.state, categoryId, request, this.actorSubject, this.clock);
  }

  async moveCategories(request: MoveCategoriesRequest): Promise<void> {
    moveCategories(this.state, request);
  }

  async reorderCategories(request: ReorderCategoriesRequest): Promise<void> {
    reorderCategories(this.state, request);
  }

  async subtreeDeleteCategory(categoryId: string, request: SubtreeDeleteRequest): Promise<SubtreeDeleteResult> {
    return { deletionOperationId: subtreeDeleteCategory(this.state, this.idFactory, categoryId, request) };
  }

  async operationRestoreCategory(deletionOperationId: string, request: OperationRestoreRequest): Promise<void> {
    operationRestoreCategory(this.state, deletionOperationId, request);
  }

  async addCategoryAlias(categoryId: string, request: AddCategoryAliasRequest): Promise<AddCategoryAliasResult> {
    return { aliasId: addCategoryAlias(this.state, this.idFactory, categoryId, request) };
  }

  async editCategoryAlias(aliasId: string, request: EditCategoryAliasRequest): Promise<void> {
    editCategoryAlias(this.state, aliasId, request);
  }

  async removeCategoryAlias(aliasId: string, request: RemoveCategoryAliasRequest): Promise<void> {
    removeCategoryAlias(this.state, aliasId, request);
  }

  async applyManualProtection(categoryId: string, protectionType: ManualProtectionType, request: ApplyManualProtectionRequest): Promise<void> {
    applyManualProtection(this.state, this.idFactory, categoryId, protectionType, request);
  }

  async liftManualProtection(categoryId: string, protectionType: ManualProtectionType, request: LiftManualProtectionRequest): Promise<void> {
    liftManualProtection(this.state, categoryId, protectionType, request);
  }

  async applyFullProtectionPreset(categoryId: string, request: ApplyFullProtectionPresetRequest): Promise<void> {
    applyFullProtectionPreset(this.state, this.idFactory, categoryId, request);
  }

  private toCategoryDto(categoryId: string): CategorySnapshotDto {
    const category = this.state.categories.get(categoryId)!;
    const resolutions = resolveProtectionProfile(this.state, categoryId, this.clock).manualProtections.filter(
      (resolution) => resolution.isProtected,
    );
    const aliases = [...this.state.aliases.values()]
      .filter((alias) => !alias.deleted && alias.categoryId === categoryId)
      .map((alias) => ({ categorySearchAliasId: alias.aliasId, value: alias.value, version: alias.version }));

    return {
      categoryId: category.categoryId,
      name: category.name,
      normalizedName: category.normalizedName,
      description: category.description,
      representativeQuranExcerpt: category.representativeQuranExcerpt,
      parentCategoryId: category.parentCategoryId,
      sectionId: category.sectionId,
      siblingOrder: category.siblingOrder,
      sectionOrder: category.sectionOrder,
      globalOrder: category.globalOrder,
      ancestorIds: category.ancestorIds,
      depth: category.depth,
      categoryContentRevision: category.categoryContentRevision,
      version: category.version,
      aliases,
      protection: {
        hasEffectiveManualProtection: resolutions.length > 0,
        isActionBlocked: resolutions.length > 0,
        manualProtections: resolutions,
      },
    };
  }
}

function toSectionDto(section: { sectionId: string; name: string; normalizedName: string; sortOrder: number; isPermanentDefault: boolean; version: number }): SectionSnapshotDto {
  return {
    sectionId: section.sectionId,
    name: section.name,
    normalizedName: section.normalizedName,
    sortOrder: section.sortOrder,
    isPermanentDefault: section.isPermanentDefault,
    version: section.version,
  };
}
