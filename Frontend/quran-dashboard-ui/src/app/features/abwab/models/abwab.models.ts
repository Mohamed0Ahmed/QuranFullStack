import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';

export type AbwabView = 'tree' | 'cards';

const ABWAB_VIEWS: ReadonlySet<string> = new Set<AbwabView>(['tree', 'cards']);

export function isAbwabView(value: unknown): value is AbwabView {
  return typeof value === 'string' && ABWAB_VIEWS.has(value);
}

export function isPositiveId(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0;
}

/** One tree row, built from `AbwabTreeDoorDto` plus its computed nesting (state/abwab-tree.builder.ts). */
export interface AbwabNode {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly representativeAyahText: string | null;
  readonly aliases: readonly string[];
  readonly sectionId: number | null;
  readonly parentId: number | null;
  readonly orderValue: number;
  readonly version: number;
  readonly isArchived: boolean;
  readonly depth: number;
  /** Direct live (non-archived) children count — drives the tree's `.count` badge. */
  readonly liveChildCount: number;
  readonly children: readonly AbwabNode[];
}

/** The builder's output: one snapshot split into the live tree and the archive tree. */
export interface AbwabTreeSnapshotVm {
  readonly sections: readonly AbwabTreeSectionDto[];
  readonly liveRoots: readonly AbwabNode[];
  readonly archivedRoots: readonly AbwabNode[];
  /** O(1) lookup for selection rebinding and search-ancestor walks. */
  readonly byId: ReadonlyMap<number, AbwabNode>;
  /** Diagnostics only — never used for conflict detection (plan-slice-b.md §7 T407). */
  readonly version: string | null;
}

/** Stable URL query keys (plan-slice-b.md §4.4) — never the translated label. */
export const ABWAB_QUERY_KEYS = {
  section: 'section',
  view: 'view',
  archive: 'archive',
  door: 'door',
  card: 'card',
  q: 'q',
} as const;

export interface AbwabQueryState {
  readonly section: number | null;
  readonly view: AbwabView;
  readonly archive: boolean;
  readonly door: number | null;
  readonly card: number | null;
  readonly q: string;
}

/** Every key fails closed to these when absent or invalid (plan-slice-b.md §4.4). */
export const ABWAB_QUERY_DEFAULTS: AbwabQueryState = {
  section: null,
  view: 'tree',
  archive: false,
  door: null,
  card: null,
  q: '',
};
