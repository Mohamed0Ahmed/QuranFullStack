import {
  AddTemplateNodeAliasRequest,
  EditTemplateNodeAliasRequest,
  RemoveTemplateNodeAliasRequest,
  RestoreTemplateNodeAliasRequest,
} from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { AbwabTemplatesMockState, assertGeneration, requireAlias } from './abwab-templates-mock-state';

export function addNodeAlias(
  state: AbwabTemplatesMockState,
  idFactory: () => string,
  templateNodeId: string,
  request: AddTemplateNodeAliasRequest,
): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  const node = state.nodes.get(templateNodeId);
  if (!node || node.isDeleted) {
    throw new AbwabConflictError('abwab.row_stale');
  }

  const templateNodeSearchAliasId = idFactory();
  state.aliases.set(templateNodeSearchAliasId, {
    templateNodeSearchAliasId,
    templateNodeId,
    value: request.value,
    isDeleted: false,
    version: 1,
  });
  return templateNodeSearchAliasId;
}

export function editNodeAlias(state: AbwabTemplatesMockState, aliasId: string, request: EditTemplateNodeAliasRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const alias = requireAlias(state, aliasId, request.expectedVersion, { isDeleted: false });

  alias.value = request.value;
  alias.version += 1;
}

export function removeNodeAlias(state: AbwabTemplatesMockState, aliasId: string, request: RemoveTemplateNodeAliasRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const alias = requireAlias(state, aliasId, request.expectedVersion, { isDeleted: false });

  alias.isDeleted = true;
  alias.version += 1;
}

export function restoreNodeAlias(state: AbwabTemplatesMockState, aliasId: string, request: RestoreTemplateNodeAliasRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const alias = requireAlias(state, aliasId, request.expectedVersion, { isDeleted: true });

  alias.isDeleted = false;
  alias.version += 1;
}
