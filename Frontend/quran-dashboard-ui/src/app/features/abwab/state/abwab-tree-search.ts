import { normalizeArabicForSearch } from '../../../shared/quran/arabic-search-normalize';

import { AbwabNode } from '../models/abwab.models';

export interface AbwabSearchOptions {
  readonly hideUnrelatedRoots?: boolean;
  readonly omittedSubtreeIds?: ReadonlySet<number>;
}

export interface AbwabSearchResult {
  readonly isFiltering: boolean;
  readonly matchedIds: ReadonlySet<number>;
  readonly matches: readonly AbwabNode[];
  readonly matchingRootIds: ReadonlySet<number>;
  readonly autoExpandedIds: ReadonlySet<number>;
  readonly displayRoots: readonly AbwabNode[];
}

export function searchAbwabNodes(
  roots: readonly AbwabNode[],
  query: string,
  options: AbwabSearchOptions = {},
): AbwabSearchResult {
  const eligibleRoots = omitAbwabSubtrees(roots, options.omittedSubtreeIds ?? new Set());
  const normalizedQuery = normalizeArabicForSearch(query.trim());
  if (normalizedQuery === '') {
    return {
      isFiltering: false,
      matchedIds: new Set(),
      matches: [],
      matchingRootIds: new Set(),
      autoExpandedIds: new Set(),
      displayRoots: eligibleRoots,
    };
  }

  const matchedIds = new Set<number>();
  const matches: AbwabNode[] = [];
  const matchingRootIds = new Set<number>();
  const pathExpandedIds = new Set<number>();
  const ancestors: AbwabNode[] = [];

  const walk = (node: AbwabNode): boolean => {
    const isMatch = nodeMatchesQuery(node, normalizedQuery);
    if (isMatch) {
      matchedIds.add(node.id);
      matches.push(node);
      ancestors.forEach((ancestor) => pathExpandedIds.add(ancestor.id));
    }

    ancestors.push(node);
    let subtreeMatches = isMatch;
    for (const child of node.children) {
      subtreeMatches = walk(child) || subtreeMatches;
    }
    ancestors.pop();
    return subtreeMatches;
  };

  for (const root of eligibleRoots) {
    if (walk(root)) {
      matchingRootIds.add(root.id);
    }
  }

  const hideUnrelatedRoots = options.hideUnrelatedRoots === true;
  const displayRoots = hideUnrelatedRoots
    ? eligibleRoots.filter((root) => matchingRootIds.has(root.id))
    : eligibleRoots;

  return {
    isFiltering: true,
    matchedIds,
    matches,
    matchingRootIds,
    autoExpandedIds: pathExpandedIds,
    displayRoots,
  };
}

function nodeMatchesQuery(node: AbwabNode, normalizedQuery: string): boolean {
  return [node.name, ...node.aliases]
    .some((candidate) => normalizeArabicForSearch(candidate).includes(normalizedQuery));
}

function omitAbwabSubtrees(
  nodes: readonly AbwabNode[],
  omittedIds: ReadonlySet<number>,
): readonly AbwabNode[] {
  if (omittedIds.size === 0) {
    return nodes;
  }

  let changed = false;
  const result: AbwabNode[] = [];
  for (const node of nodes) {
    if (omittedIds.has(node.id)) {
      changed = true;
      continue;
    }
    const children = omitAbwabSubtrees(node.children, omittedIds);
    if (children !== node.children) {
      changed = true;
      result.push({ ...node, children });
      continue;
    }
    result.push(node);
  }
  return changed ? result : nodes;
}
