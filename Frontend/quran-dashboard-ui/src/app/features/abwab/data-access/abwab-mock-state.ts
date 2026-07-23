import { ManualProtectionScope, ManualProtectionType } from '../../../core/api/generated/models';
import { normalizeArabicNameForUi } from './abwab-mock-normalize';

export interface MockSectionRow {
  sectionId: string;
  name: string;
  normalizedName: string;
  sortOrder: number;
  isPermanentDefault: boolean;
  version: number;
  deleted: boolean;
}

export interface MockCategoryRow {
  categoryId: string;
  name: string;
  normalizedName: string;
  description: string | null;
  representativeQuranExcerpt: string | null;
  parentCategoryId: string | null;
  sectionId: string | null;
  siblingOrder: number | null;
  sectionOrder: number | null;
  globalOrder: number | null;
  ancestorIds: string[];
  depth: number;
  categoryContentRevision: number;
  version: number;
  deleted: boolean;
  deletionOperationId: string | null;
  lastEditedAtUtc: string | null;
  lastEditorSubject: string | null;
}

export interface MockAliasRow {
  aliasId: string;
  categoryId: string;
  value: string;
  normalizedValue: string;
  version: number;
  deleted: boolean;
}

export interface MockManualProtectionRow {
  manualProtectionId: string;
  categoryId: string;
  protectionType: ManualProtectionType;
  scope: ManualProtectionScope;
  version: number;
  active: boolean;
}

export interface AbwabMockState {
  sections: Map<string, MockSectionRow>;
  categories: Map<string, MockCategoryRow>;
  aliases: Map<string, MockAliasRow>;
  manualProtections: Map<string, MockManualProtectionRow>;
  treeRevision: number;
  timelineGeneration: number;
  nextRootSectionOrder: number;
  nextRootGlobalOrder: number;
}

export const PERMANENT_DEFAULT_SECTION_NAME = 'أبواب غير مصنفة';

export function createSeededMockState(idFactory: () => string): AbwabMockState {
  const permanentDefaultId = idFactory();
  const sections = new Map<string, MockSectionRow>();
  sections.set(permanentDefaultId, {
    sectionId: permanentDefaultId,
    name: PERMANENT_DEFAULT_SECTION_NAME,
    normalizedName: normalizeArabicNameForUi(PERMANENT_DEFAULT_SECTION_NAME),
    sortOrder: 0,
    isPermanentDefault: true,
    version: 1,
    deleted: false,
  });

  return {
    sections,
    categories: new Map(),
    aliases: new Map(),
    manualProtections: new Map(),
    treeRevision: 1,
    timelineGeneration: 1,
    nextRootSectionOrder: 0,
    nextRootGlobalOrder: 0,
  };
}

export function permanentDefaultSectionId(state: AbwabMockState): string {
  for (const section of state.sections.values()) {
    if (section.isPermanentDefault) {
      return section.sectionId;
    }
  }
  throw new Error('Mock state invariant violated: no permanent default section seeded.');
}
