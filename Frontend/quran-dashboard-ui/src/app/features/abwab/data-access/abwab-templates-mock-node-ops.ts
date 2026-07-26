import {
  AddTemplateNodeRequest,
  EditTemplateNodeRequest,
  RemoveTemplateNodeRequest,
  ReorderTemplateNodesRequest,
  ReparentTemplateNodeRequest,
} from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import {
  AbwabTemplatesMockState,
  MockTemplateNodeRow,
  assertGeneration,
  compactSiblingOrders,
  requireActiveTemplate,
  requireNode,
  siblings,
  subtree,
} from './abwab-templates-mock-state';

export function addNode(
  state: AbwabTemplatesMockState,
  idFactory: () => string,
  doorTemplateId: string,
  request: AddTemplateNodeRequest,
): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  const template = requireActiveTemplate(state, doorTemplateId, request.expectedTemplateRevision);

  const templateNodeId = idFactory();
  state.nodes.set(templateNodeId, {
    templateNodeId,
    doorTemplateId,
    parentTemplateNodeId: request.parentTemplateNodeId,
    name: request.name,
    representativeQuranExcerpt: request.representativeQuranExcerpt,
    description: request.description,
    siblingOrder: siblings(state, doorTemplateId, request.parentTemplateNodeId).length,
    isDeleted: false,
    version: 1,
  });

  template.templateRevision += 1;
  return templateNodeId;
}

// Content-only: it changes no structure, so it bumps no TemplateRevision — mirroring the writer.
export function editNode(state: AbwabTemplatesMockState, templateNodeId: string, request: EditTemplateNodeRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const node = requireNode(state, templateNodeId, request.expectedVersion);

  node.name = request.name;
  node.representativeQuranExcerpt = request.representativeQuranExcerpt;
  node.description = request.description;
  node.version += 1;
}

export function reparentNode(state: AbwabTemplatesMockState, templateNodeId: string, request: ReparentTemplateNodeRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const node = requireNode(state, templateNodeId, request.expectedVersion);
  const template = requireActiveTemplate(state, node.doorTemplateId, request.expectedTemplateRevision);

  assertNoCycle(state, node, request.newParentTemplateNodeId);

  const previousParentId = node.parentTemplateNodeId;
  node.parentTemplateNodeId = request.newParentTemplateNodeId;
  node.siblingOrder = siblings(state, node.doorTemplateId, request.newParentTemplateNodeId, templateNodeId).length;
  node.version += 1;

  compactSiblingOrders(state, node.doorTemplateId, previousParentId, templateNodeId);
  template.templateRevision += 1;
}

export function reorderNodes(state: AbwabTemplatesMockState, doorTemplateId: string, request: ReorderTemplateNodesRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const template = requireActiveTemplate(state, doorTemplateId, request.expectedTemplateRevision);
  const current = siblings(state, doorTemplateId, request.parentTemplateNodeId);

  // A duplicated or empty list is malformed input rejected by the request contract as an HTTP 400,
  // never an abwab.* conflict; only a genuine count/membership mismatch is observed staleness.
  if (request.orderedTemplateNodeIds.length === 0 ||
    new Set(request.orderedTemplateNodeIds).size !== request.orderedTemplateNodeIds.length) {
    throw new Error('The reorder request must list each sibling exactly once.');
  }

  if (
    request.orderedTemplateNodeIds.length !== current.length ||
    request.orderedTemplateNodeIds.some((id) => !current.some((sibling) => sibling.templateNodeId === id))
  ) {
    throw new AbwabConflictError('abwab.row_stale');
  }

  request.orderedTemplateNodeIds.forEach((id, index) => {
    state.nodes.get(id)!.siblingOrder = index;
  });
  template.templateRevision += 1;
}

export function removeNode(state: AbwabTemplatesMockState, templateNodeId: string, request: RemoveTemplateNodeRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const node = requireNode(state, templateNodeId, request.expectedVersion);
  const template = requireActiveTemplate(state, node.doorTemplateId, request.expectedTemplateRevision);

  for (const member of subtree(state, templateNodeId)) {
    member.isDeleted = true;
    member.version += 1;
  }

  compactSiblingOrders(state, node.doorTemplateId, node.parentTemplateNodeId, templateNodeId);
  template.templateRevision += 1;
}

function assertNoCycle(state: AbwabTemplatesMockState, node: MockTemplateNodeRow, newParentTemplateNodeId: string | null): void {
  if (newParentTemplateNodeId === null) {
    return;
  }
  if (newParentTemplateNodeId === node.templateNodeId) {
    throw new AbwabConflictError('abwab.template_cycle');
  }

  let current = state.nodes.get(newParentTemplateNodeId);
  const visited = new Set<string>();
  while (current) {
    if (current.templateNodeId === node.templateNodeId || visited.has(current.templateNodeId)) {
      throw new AbwabConflictError('abwab.template_cycle');
    }
    visited.add(current.templateNodeId);
    current = current.parentTemplateNodeId ? state.nodes.get(current.parentTemplateNodeId) : undefined;
  }
}
