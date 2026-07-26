import { ApplyDoorTemplateRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { AbwabTemplatesMockState, assertGeneration, requireActiveTemplate, siblings } from './abwab-templates-mock-state';

// Mirrors the one audited operation: every revalidation runs BEFORE any state changes, so a failed
// application leaves the mock exactly as it was — the same "no partial tree" the writer guarantees.
export function applyTemplate(
  state: AbwabTemplatesMockState,
  doorTemplateId: string,
  request: ApplyDoorTemplateRequest,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);

  if (!state.targetCategoryIds.has(request.targetCategoryId)) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (state.protectedCategoryIds.has(request.targetCategoryId)) {
    throw new AbwabConflictError('abwab.manual_protection');
  }
  if (request.expectedTreeRevision !== state.treeRevision) {
    throw new AbwabConflictError('abwab.tree_revision_stale');
  }

  requireActiveTemplate(state, doorTemplateId, request.expectedTemplateRevision);

  const taken = state.takenChildNames[request.targetCategoryId] ?? [];
  if (siblings(state, doorTemplateId, null).some((root) => taken.includes(root.name))) {
    throw new AbwabConflictError('abwab.category_name_conflict');
  }

  state.treeRevision += 1;
}
