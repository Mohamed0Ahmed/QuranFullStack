import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateNodeDto } from '../../../core/api/generated/models/abwab-template-node-dto';

/** One row in the workshop's template list. `nodeCount` counts the root's live descendants,
 * excluding the root itself — the contract's «N عناصر» chip (`abwab-templates-concept.html:186`). */
export interface AbwabTemplateSummaryVm {
  readonly id: number;
  readonly name: string;
  readonly nodeCount: number;
}

/** One node of the template tree, built from the flat `AbwabTemplateDto.nodes` list. */
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
  /** Exactly one root per template, enforced by a partial unique index (plan §5.2). `null` only
   * for a template whose root the reader could not resolve, which the backend treats as
   * not-found — so the workshop never renders a rootless template. */
  readonly root: AbwabTemplateNodeVm | null;
  /** Live descendants of the root, matching the list's «N عناصر» chip. */
  readonly nodeCount: number;
}

/** The four fields every door and every template node is authored through — the value type the
 * shared authoring form reads and emits. */
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

/** Flat → tree, the `abwab-tree.builder.ts` shape: siblings sort by `orderValue` then `id`, so a
 * gap or a tie left by a concurrent write still renders in a stable order. */
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

/** O(1) lookup for the workshop's action handlers, which know a node id and need its row. */
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
