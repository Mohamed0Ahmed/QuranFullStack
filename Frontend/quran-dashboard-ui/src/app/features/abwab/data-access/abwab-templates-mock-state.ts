import { AbwabConflictError } from './abwab-conflict';

export interface MockTemplateRow {
  doorTemplateId: string;
  name: string;
  description: string | null;
  templateRevision: number;
  isDeleted: boolean;
  version: number;
}

export interface MockTemplateNodeRow {
  templateNodeId: string;
  doorTemplateId: string;
  parentTemplateNodeId: string | null;
  name: string;
  representativeQuranExcerpt: string | null;
  description: string | null;
  siblingOrder: number;
  isDeleted: boolean;
  version: number;
}

export interface MockTemplateAliasRow {
  templateNodeSearchAliasId: string;
  templateNodeId: string;
  value: string;
  isDeleted: boolean;
  version: number;
}

export interface AbwabTemplatesMockState {
  readonly templates: Map<string, MockTemplateRow>;
  readonly nodes: Map<string, MockTemplateNodeRow>;
  readonly aliases: Map<string, MockTemplateAliasRow>;
  readonly targetCategoryIds: Set<string>;
  readonly protectedCategoryIds: Set<string>;
  readonly takenChildNames: Readonly<Record<string, readonly string[]>>;
  timelineGeneration: number;
  treeRevision: number;
}

export function assertGeneration(state: AbwabTemplatesMockState, expected: number): void {
  if (expected !== state.timelineGeneration) {
    throw new AbwabConflictError('abwab.timeline_generation_stale');
  }
}

export function requireTemplate(state: AbwabTemplatesMockState, doorTemplateId: string): MockTemplateRow {
  const template = state.templates.get(doorTemplateId);
  if (!template) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  return template;
}

export function requireTemplateVersion(
  state: AbwabTemplatesMockState,
  doorTemplateId: string,
  expectedVersion: number,
  expected: { isDeleted: boolean },
): MockTemplateRow {
  const template = requireTemplate(state, doorTemplateId);
  if (template.version !== expectedVersion || template.isDeleted !== expected.isDeleted) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  return template;
}

export function requireActiveTemplate(
  state: AbwabTemplatesMockState,
  doorTemplateId: string,
  expectedTemplateRevision: number,
): MockTemplateRow {
  const template = requireTemplate(state, doorTemplateId);
  if (template.isDeleted) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  if (template.templateRevision !== expectedTemplateRevision) {
    throw new AbwabConflictError('abwab.template_revision_stale');
  }
  return template;
}

export function requireNode(state: AbwabTemplatesMockState, templateNodeId: string, expectedVersion: number): MockTemplateNodeRow {
  const node = state.nodes.get(templateNodeId);
  if (!node || node.isDeleted || node.version !== expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  return node;
}

export function requireAlias(
  state: AbwabTemplatesMockState,
  aliasId: string,
  expectedVersion: number,
  expected: { isDeleted: boolean },
): MockTemplateAliasRow {
  const alias = state.aliases.get(aliasId);
  if (!alias || alias.version !== expectedVersion || alias.isDeleted !== expected.isDeleted) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  return alias;
}

export function siblings(
  state: AbwabTemplatesMockState,
  doorTemplateId: string,
  parentTemplateNodeId: string | null,
  excludeTemplateNodeId?: string,
): MockTemplateNodeRow[] {
  return [...state.nodes.values()]
    .filter(
      (node) =>
        node.doorTemplateId === doorTemplateId &&
        !node.isDeleted &&
        node.parentTemplateNodeId === parentTemplateNodeId &&
        node.templateNodeId !== excludeTemplateNodeId,
    )
    .sort((left, right) => left.siblingOrder - right.siblingOrder);
}

export function compactSiblingOrders(
  state: AbwabTemplatesMockState,
  doorTemplateId: string,
  parentTemplateNodeId: string | null,
  excludeTemplateNodeId: string,
): void {
  siblings(state, doorTemplateId, parentTemplateNodeId, excludeTemplateNodeId).forEach((sibling, index) => {
    sibling.siblingOrder = index;
  });
}

export function subtree(state: AbwabTemplatesMockState, rootTemplateNodeId: string): MockTemplateNodeRow[] {
  const members: MockTemplateNodeRow[] = [];
  let frontier = [rootTemplateNodeId];

  while (frontier.length > 0) {
    const layer = [...state.nodes.values()].filter(
      (node) =>
        !node.isDeleted &&
        (frontier.includes(node.templateNodeId) ||
          (node.parentTemplateNodeId !== null && frontier.includes(node.parentTemplateNodeId))) &&
        !members.some((member) => member.templateNodeId === node.templateNodeId),
    );

    if (layer.length === 0) {
      break;
    }

    members.push(...layer);
    frontier = layer.map((node) => node.templateNodeId);
  }

  return members;
}
