import { normalizeArabicForSearch } from '../../../shared/quran/arabic-search-normalize';

import { AbwabNode } from '../models/abwab.models';

export interface AbwabSearchOptions {
  readonly hideUnrelatedRoots?: boolean;
  readonly omittedSubtreeIds?: ReadonlySet<number>;
  readonly autoExpandMatches?: boolean;
}

export interface AbwabSearchMatch {
  readonly id: number;
  readonly displayLabel: string;
}

export interface AbwabSearchResult {
  readonly isFiltering: boolean;
  readonly matchedIds: ReadonlySet<number>;
  readonly matches: readonly AbwabSearchMatch[];
  readonly matchingRootIds: ReadonlySet<number>;
  readonly autoExpandedIds: ReadonlySet<number>;
  readonly displayRoots: readonly AbwabNode[];
}

export const ABWAB_SEARCH_MIN_CHARACTERS = 2;

const normalizedCandidatesByNode = new WeakMap<AbwabNode, readonly string[]>();

export function searchAbwabNodes(
  roots: readonly AbwabNode[],
  query: string,
  options: AbwabSearchOptions = {},
): AbwabSearchResult {
  const eligibleRoots = omitAbwabSubtrees(roots, options.omittedSubtreeIds ?? new Set());
  const normalizedQuery = normalizeArabicForSearch(query.trim());
  if (Array.from(normalizedQuery).length < ABWAB_SEARCH_MIN_CHARACTERS) {
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
  const matches: AbwabSearchMatch[] = [];
  const matchingRootIds = new Set<number>();
  const pathExpandedIds = new Set<number>();
  const ancestors: AbwabNode[] = [];

  const walk = (node: AbwabNode): boolean => {
    const isMatch = nodeMatchesQuery(node, normalizedQuery);
    if (isMatch) {
      matchedIds.add(node.id);
      const parent = ancestors.at(-1);
      matches.push({
        id: node.id,
        displayLabel: parent ? `${parent.name} ← ${node.name}` : node.name,
      });
      if (options.autoExpandMatches !== false) {
        ancestors.forEach((ancestor) => pathExpandedIds.add(ancestor.id));
      }
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
  let normalizedCandidates = normalizedCandidatesByNode.get(node);
  if (!normalizedCandidates) {
    normalizedCandidates = [node.name, ...node.aliases].map(normalizeArabicForSearch);
    normalizedCandidatesByNode.set(node, normalizedCandidates);
  }
  return normalizedCandidates.some((candidate) => candidate.includes(normalizedQuery));
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
