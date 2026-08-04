import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateNodeDto } from '../../../core/api/generated/models/abwab-template-node-dto';

export interface AbwabTemplateNodeVm {
  readonly id: number;
  readonly parentNodeId: number | null;
  readonly name: string;
  readonly description: string | null;
  readonly representativeAyahText: string | null;
  readonly aliases: readonly string[];
  readonly orderValue: number;
  readonly depth: number;
  readonly children: readonly AbwabTemplateNodeVm[];
}

export interface AbwabTemplateVm {
  readonly id: number;
  readonly name: string;
  readonly root: AbwabTemplateNodeVm | null;
  readonly nodeCount: number;
}

export interface AbwabAuthoringFields {
  readonly name: string;
  readonly description: string;
  readonly representativeAyahText: string;
  readonly aliases: readonly string[];
}

export const EMPTY_AUTHORING_FIELDS: AbwabAuthoringFields = {
  name: '',
  description: '',
  representativeAyahText: '',
  aliases: [],
};

function byOrderThenId(a: AbwabTemplateNodeDto, b: AbwabTemplateNodeDto): number {
  return a.orderValue - b.orderValue || a.id - b.id;
}

export function buildAbwabTemplateTree(dto: AbwabTemplateDto): AbwabTemplateVm {
  const childrenByParentId = new Map<number, AbwabTemplateNodeDto[]>();
  let rootDto: AbwabTemplateNodeDto | null = null;

  for (const node of dto.nodes) {
    if (node.parentNodeId == null) {
      rootDto = node;
      continue;
    }
    const siblings = childrenByParentId.get(node.parentNodeId) ?? [];
    siblings.push(node);
    childrenByParentId.set(node.parentNodeId, siblings);
  }
  for (const siblings of childrenByParentId.values()) {
    siblings.sort(byOrderThenId);
  }

  let descendantCount = 0;

  function build(node: AbwabTemplateNodeDto, depth: number): AbwabTemplateNodeVm {
    const children = (childrenByParentId.get(node.id) ?? []).map((child) => {
      descendantCount += 1;
      return build(child, depth + 1);
    });
    return {
      id: node.id,
      parentNodeId: node.parentNodeId,
      name: node.name,
      description: node.description,
      representativeAyahText: node.representativeAyahText,
      aliases: node.aliases,
      orderValue: node.orderValue,
      depth,
      children,
    };
  }

  const root = rootDto === null ? null : build(rootDto, 0);
  return { id: dto.id, name: dto.name, root, nodeCount: descendantCount };
}

export function collectAbwabTemplateNodes(root: AbwabTemplateNodeVm | null): ReadonlyMap<number, AbwabTemplateNodeVm> {
  const byId = new Map<number, AbwabTemplateNodeVm>();
  const walk = (node: AbwabTemplateNodeVm): void => {
    byId.set(node.id, node);
    node.children.forEach(walk);
  };
  if (root) {
    walk(root);
  }
  return byId;
}

export function toAuthoringFields(node: AbwabTemplateNodeVm): AbwabAuthoringFields {
  return {
    name: node.name,
    description: node.description ?? '',
    representativeAyahText: node.representativeAyahText ?? '',
    aliases: node.aliases,
  };
}
