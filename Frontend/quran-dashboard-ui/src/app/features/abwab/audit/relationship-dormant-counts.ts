import { RelationshipType } from '../../../core/api/generated/models';
import { DormantDependentCount } from './abwab-audit-render.models';

export interface DormantRelationshipCount {
  readonly relationshipType: RelationshipType;
  readonly count: number;
}

export interface DormantRelationshipCounts {
  readonly totalDormant: number;
  readonly byType: readonly DormantRelationshipCount[];
}

// Labels arrive as an argument rather than an import: this module is shared code, and an import edge
// from audit/ to the relationship label module puts that module in a chunk the page component's own
// chunk then reads before it is initialised — the page renders its type <select> with zero options.
// Passing them keeps ONE label source (data-access/relationship-type-labels.ts) with no shared edge.
//
// Contributes the relationship NAME only; the 029-owned subtree render seam already stamps every
// entry with its own «خامل» badge, so repeating it here would render the word twice.
export function toDormantDependentCounts(
  counts: DormantRelationshipCounts,
  typeLabels: Record<RelationshipType, string>,
): readonly DormantDependentCount[] {
  return counts.byType
    .filter((entry) => entry.count > 0)
    .map((entry) => ({
      label: `علاقات (${typeLabels[entry.relationshipType]})`,
      count: entry.count,
    }));
}
