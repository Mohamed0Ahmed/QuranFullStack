import { AddSectionRequest, DeleteSectionRequest, EditSectionRequest, ReorderSectionsRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { normalizeArabicNameForUi } from './abwab-mock-normalize';
import { AbwabMockState } from './abwab-mock-state';
import { assertGeneration, assertTreeRevision, bumpTreeRevision, isCategoryActiveRootOfSection } from './abwab-mock-shared';

export function addSection(state: AbwabMockState, idFactory: () => string, request: AddSectionRequest): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  const normalizedName = normalizeArabicNameForUi(request.name);
  const conflict = [...state.sections.values()].some((section) => !section.deleted && section.normalizedName === normalizedName);
  if (conflict) {
    throw new AbwabConflictError('abwab.section_name_conflict');
  }

  const sectionId = idFactory();
  const maxOrder = Math.max(-1, ...[...state.sections.values()].filter((s) => !s.deleted).map((s) => s.sortOrder));
  state.sections.set(sectionId, {
    sectionId,
    name: request.name,
    normalizedName,
    sortOrder: maxOrder + 1,
    isPermanentDefault: false,
    version: 1,
    deleted: false,
  });
  bumpTreeRevision(state);
  return sectionId;
}

export function editSection(state: AbwabMockState, sectionId: string, request: EditSectionRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);
  const section = state.sections.get(sectionId);
  if (!section || section.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (section.isPermanentDefault) {
    throw new AbwabConflictError('abwab.permanent_default_section');
  }
  if (section.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }

  const normalizedName = normalizeArabicNameForUi(request.name);
  const conflict = [...state.sections.values()].some(
    (other) => other.sectionId !== sectionId && !other.deleted && other.normalizedName === normalizedName,
  );
  if (conflict) {
    throw new AbwabConflictError('abwab.section_name_conflict');
  }

  section.name = request.name;
  section.normalizedName = normalizedName;
  section.version += 1;
  bumpTreeRevision(state);
}

export function reorderSections(state: AbwabMockState, request: ReorderSectionsRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  for (const order of request.orders) {
    const section = state.sections.get(order.sectionId);
    if (!section || section.deleted) {
      throw new AbwabConflictError('abwab.category_unavailable');
    }
    if (section.version !== order.expectedVersion) {
      throw new AbwabConflictError('abwab.row_stale');
    }
  }
  for (const order of request.orders) {
    const section = state.sections.get(order.sectionId)!;
    section.sortOrder = order.sortOrder;
    section.version += 1;
  }
  bumpTreeRevision(state);
}

export function deleteSection(state: AbwabMockState, sectionId: string, request: DeleteSectionRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);
  const section = state.sections.get(sectionId);
  if (!section || section.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (section.isPermanentDefault) {
    throw new AbwabConflictError('abwab.permanent_default_section');
  }
  if (section.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  if (isCategoryActiveRootOfSection(state, sectionId)) {
    throw new AbwabConflictError('abwab.section_not_empty');
  }
  section.deleted = true;
  bumpTreeRevision(state);
}
