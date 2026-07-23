import {
  ApplyFullProtectionPresetRequest,
  ApplyManualProtectionRequest,
  CategoryProtectionProfileDto,
  LiftManualProtectionRequest,
  ManualProtectionResolutionDto,
  ManualProtectionType,
} from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { AbwabMockState, MockManualProtectionRow } from './abwab-mock-state';
import { assertGeneration, MockClock } from './abwab-mock-shared';

const PROTECTION_TYPES: readonly ManualProtectionType[] = [0, 1, 2, 3, 4];
const PRESET_TYPE_KEYS: readonly (keyof ApplyFullProtectionPresetRequest['expectedVersions'])[] = [
  'CategoryData',
  'InternalStructure',
  'QuranContent',
  'Deletion',
  'Relationship',
];

function findActiveRecord(state: AbwabMockState, categoryId: string, protectionType: ManualProtectionType): MockManualProtectionRow | undefined {
  for (const record of state.manualProtections.values()) {
    if (record.categoryId === categoryId && record.protectionType === protectionType && record.active) {
      return record;
    }
  }
  return undefined;
}

function resolveOneType(state: AbwabMockState, categoryId: string, protectionType: ManualProtectionType, serverTimeUtc: string): ManualProtectionResolutionDto {
  const direct = findActiveRecord(state, categoryId, protectionType);
  if (direct) {
    return {
      protectionType,
      isProtected: true,
      isDirect: true,
      scope: direct.scope,
      sourceCategoryId: categoryId,
      serverTimeUtc,
      actionClassification: 2,
      manualProtectionId: direct.manualProtectionId,
      version: direct.version,
    };
  }

  const category = state.categories.get(categoryId);
  const ancestorIds = category?.ancestorIds ?? [];
  for (let index = ancestorIds.length - 1; index >= 0; index -= 1) {
    const ancestorId = ancestorIds[index];
    const inherited = findActiveRecord(state, ancestorId, protectionType);
    if (inherited && inherited.scope === 1) {
      return {
        protectionType,
        isProtected: true,
        isDirect: false,
        scope: inherited.scope,
        sourceCategoryId: ancestorId,
        serverTimeUtc,
        actionClassification: 2,
        manualProtectionId: null,
        version: null,
      };
    }
  }

  return {
    protectionType,
    isProtected: false,
    isDirect: false,
    scope: null,
    sourceCategoryId: null,
    serverTimeUtc,
    actionClassification: 0,
    manualProtectionId: null,
    version: null,
  };
}

export function resolveProtectionProfile(state: AbwabMockState, categoryId: string, clock: MockClock): CategoryProtectionProfileDto {
  const serverTimeUtc = clock.nowUtc();
  const category = state.categories.get(categoryId);
  const manualProtections = PROTECTION_TYPES.map((type) => resolveOneType(state, categoryId, type, serverTimeUtc));

  return {
    categoryId,
    serverTimeUtc,
    expectedTimelineGeneration: { generation: state.timelineGeneration },
    manualProtections,
    ordinaryProtection: {
      isActive: false,
      actorSubject: category?.lastEditorSubject ?? null,
      lastEditedAtUtc: category?.lastEditedAtUtc ?? null,
      expiresAtUtc: null,
    },
  };
}

export function applyManualProtection(
  state: AbwabMockState,
  idFactory: () => string,
  categoryId: string,
  protectionType: ManualProtectionType,
  request: ApplyManualProtectionRequest,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const existing = findActiveRecord(state, categoryId, protectionType);

  if (!existing) {
    const manualProtectionId = idFactory();
    state.manualProtections.set(manualProtectionId, {
      manualProtectionId,
      categoryId,
      protectionType,
      scope: request.scope,
      version: 1,
      active: true,
    });
    return;
  }

  if (existing.scope === request.scope) {
    return;
  }

  if (request.expectedVersion !== existing.version) {
    throw new AbwabConflictError('abwab.manual_protection_scope_conflict');
  }
  existing.scope = request.scope;
  existing.version += 1;
}

export function liftManualProtection(
  state: AbwabMockState,
  categoryId: string,
  protectionType: ManualProtectionType,
  request: LiftManualProtectionRequest,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const existing = findActiveRecord(state, categoryId, protectionType);
  if (!existing) {
    return;
  }
  if (request.expectedVersion !== existing.version) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  existing.active = false;
  existing.version += 1;
}

export function applyFullProtectionPreset(
  state: AbwabMockState,
  idFactory: () => string,
  categoryId: string,
  request: ApplyFullProtectionPresetRequest,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);

  // Validate every changed scope BEFORE mutating any record, so a single stale scope rolls back
  // the entire five-type command (manual-protection-contract.md).
  for (let index = 0; index < PROTECTION_TYPES.length; index += 1) {
    const protectionType = PROTECTION_TYPES[index];
    const existing = findActiveRecord(state, categoryId, protectionType);
    if (existing && existing.scope !== request.scope) {
      const expectedVersion = request.expectedVersions[PRESET_TYPE_KEYS[index]];
      if (expectedVersion !== existing.version) {
        throw new AbwabConflictError('abwab.manual_protection_scope_conflict');
      }
    }
  }

  for (const protectionType of PROTECTION_TYPES) {
    const existing = findActiveRecord(state, categoryId, protectionType);
    if (!existing) {
      const manualProtectionId = idFactory();
      state.manualProtections.set(manualProtectionId, {
        manualProtectionId,
        categoryId,
        protectionType,
        scope: request.scope,
        version: 1,
        active: true,
      });
    } else if (existing.scope !== request.scope) {
      existing.scope = request.scope;
      existing.version += 1;
    }
  }
}
