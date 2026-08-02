import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabReorderScope } from '../../../core/api/generated/models/abwab-reorder-scope';
import { AbwabRelationType } from '../../../core/api/generated/models/abwab-relation-type';
import { AbwabRelationDirection } from '../../../core/api/generated/models/abwab-relation-direction';

export type AbwabView = 'tree' | 'cards';

/**
 * The restorable overlays (plan-slice-e.md §4.2-2). Every subject is derivable from
 * `door=` plus the snapshot, which is why the key carries a kind and no id of its own —
 * the bulk modes of the move picker and the relations modal read `bulkSet`, which is not
 * URL state, and therefore never write this key.
 */
export type AbwabModalKind = 'create' | 'child' | 'edit' | 'move' | 'sections' | 'relations';

/** `modal=<kind>` while open, `modal=<kind>-closed` while retained and restorable. */
export interface AbwabModalState {
  readonly kind: AbwabModalKind;
  readonly closed: boolean;
  /** The subject this retained state belongs to, when it is NOT `door=`. Null on every open
   * state and on every plain `-closed` one — those follow `door=`, so a later selection moves
   * what restore would reopen. Only a retained `relations` may carry an id, and only because
   * a reveal deliberately points `door=` somewhere else: the id pins the subject so the
   * restore control still reopens the door the user came from. */
  readonly subjectDoorId: number | null;
}

/** Which order space a reorder acts on (plan.md §4 — two independent spaces, zero coupling).
 * `'section'` is the existing per-`(section, parent)` order; `'global'` is «كل الأبواب»'s own,
 * root-doors-only order. Kept as a readable domain type in this feature's own view models;
 * mapped to the wire's numeric `AbwabReorderScope` only at the dispatch boundary. */
export type AbwabOrderScope = 'global' | 'section';

export const ABWAB_ORDER_SCOPE_TO_WIRE: Readonly<Record<AbwabOrderScope, AbwabReorderScope>> = {
  section: 1,
  global: 2,
};

/** The three relation types, as readable names. Mapped to the wire's numeric enums only at the
 * dispatch boundary, exactly like `AbwabOrderScope` above. */
export type AbwabRelationKind = 'similarity' | 'opposition' | 'comprehensiveness';

/** Direction is stated from the ANCHOR door's side — the door whose modal is open. The design
 * contract uses "broader"/"narrower" from two different perspectives, so neither word appears here
 * or on the wire (plan §5.3). */
export type AbwabRelationDirectionKind = 'anchor-more' | 'anchor-less';

export const ABWAB_RELATION_KIND_TO_WIRE: Readonly<Record<AbwabRelationKind, AbwabRelationType>> = {
  similarity: 1,
  opposition: 2,
  comprehensiveness: 3,
};

export const ABWAB_RELATION_KIND_FROM_WIRE: Readonly<Record<AbwabRelationType, AbwabRelationKind>> = {
  1: 'similarity',
  2: 'opposition',
  3: 'comprehensiveness',
};

export const ABWAB_RELATION_DIRECTION_TO_WIRE: Readonly<
  Record<AbwabRelationDirectionKind, AbwabRelationDirection>
> = {
  'anchor-more': 1,
  'anchor-less': 2,
};

export const ABWAB_RELATION_DIRECTION_FROM_WIRE: Readonly<
  Record<AbwabRelationDirection, AbwabRelationDirectionKind>
> = {
  1: 'anchor-more',
  2: 'anchor-less',
};

/** One relation as the open modal sees it: always the OTHER door, and a direction already resolved
 * from this door's perspective by the backend. */
export interface AbwabRelationVm {
  readonly id: number;
  readonly otherDoorId: number;
  readonly otherDoorName: string;
  readonly kind: AbwabRelationKind;
  readonly direction: AbwabRelationDirectionKind | null;
}

/** Four display groups, not three types: one comprehensiveness row lands in a different group on
 * each of its two doors. */
export type AbwabRelationGroupKey = 'similarity' | 'opposition' | 'more-comprehensive' | 'less-comprehensive';

export interface AbwabRelationGroupVm {
  readonly key: AbwabRelationGroupKey;
  readonly relations: readonly AbwabRelationVm[];
}

/** Contract order (`abwab-relations-concept.html:193-198`): تشابه · تضاد · أكثر شمولية · أقل شمولية.
 * Empty groups are dropped — the contract renders only the ones that have rows. */
const ABWAB_RELATION_GROUP_ORDER: readonly AbwabRelationGroupKey[] = [
  'similarity',
  'opposition',
  'more-comprehensive',
  'less-comprehensive',
];

/** The one place §5.3's rule lives: the anchor being the more comprehensive side means the OTHER
 * door is the less comprehensive one, so the row is displayed under «أبواب أقل شمولية». */
export function abwabRelationGroupKey(relation: AbwabRelationVm): AbwabRelationGroupKey {
  if (relation.kind !== 'comprehensiveness') {
    return relation.kind;
  }
  return relation.direction === 'anchor-more' ? 'less-comprehensive' : 'more-comprehensive';
}

export function groupAbwabRelations(relations: readonly AbwabRelationVm[]): readonly AbwabRelationGroupVm[] {
  return ABWAB_RELATION_GROUP_ORDER.map((key) => ({
    key,
    relations: relations.filter((relation) => abwabRelationGroupKey(relation) === key),
  })).filter((group) => group.relations.length > 0);
}

const ABWAB_VIEWS: ReadonlySet<string> = new Set<AbwabView>(['tree', 'cards']);

export function isAbwabView(value: unknown): value is AbwabView {
  return typeof value === 'string' && ABWAB_VIEWS.has(value);
}

export function isPositiveId(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0;
}

const ABWAB_MODAL_KINDS: ReadonlySet<string> = new Set<AbwabModalKind>([
  'create',
  'child',
  'edit',
  'move',
  'sections',
  'relations',
]);

export function isAbwabModalKind(value: unknown): value is AbwabModalKind {
  return typeof value === 'string' && ABWAB_MODAL_KINDS.has(value);
}

/** The four kinds whose subject is `door=` — they parse to nothing without a valid one. */
const ABWAB_DOOR_DEPENDENT_MODAL_KINDS: ReadonlySet<AbwabModalKind> = new Set<AbwabModalKind>([
  'child',
  'edit',
  'move',
  'relations',
]);

export function isDoorDependentAbwabModalKind(kind: AbwabModalKind): boolean {
  return ABWAB_DOOR_DEPENDENT_MODAL_KINDS.has(kind);
}

/** One tree row, built from `AbwabTreeDoorDto` plus its computed nesting (state/abwab-tree.builder.ts). */
export interface AbwabNode {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly representativeAyahText: string | null;
  readonly aliases: readonly string[];
  readonly sectionId: number;
  /** Its section has been archived — true only for an archived door, since a section is only
   * archivable while it holds no live ones. Restoring such a door needs a destination chosen. */
  readonly sectionRetired: boolean;
  readonly parentId: number | null;
  readonly orderValue: number;
  /** Live root doors only — `null` at any depth > 0 and for every archived door
   * (`global_order_value IS NOT NULL ⟺ parent_id IS NULL AND deleted_at IS NULL`, plan.md §5). */
  readonly globalOrderValue: number | null;
  readonly version: number;
  readonly isArchived: boolean;
  readonly depth: number;
  /** Direct live (non-archived) children count — drives the tree's `.count` badge. */
  readonly liveChildCount: number;
  /** Every live descendant at any depth, the door itself excluded. Live-only like every other
   * count in this feature: an archived subtree never inflates it. */
  readonly liveDescendantCount: number;
  /** How many levels of live descendants hang below this door — child + grandchild +
   * great-grandchild is **3**. Relative to the door, never `depth`, and not derivable from
   * `liveDescendantCount`: a chain of three and a fan of three both count 3 descendants but
   * measure 3 and 1 here. */
  readonly maxRelativeDepth: number;
  /** Visible relations only: a relation whose other endpoint is archived is dormant and counts 0,
   * so an archived door's count is always 0 and its row never shows the flag. */
  readonly relationCount: number;
  readonly children: readonly AbwabNode[];
}

/** The builder's output: one snapshot split into the live tree and the archive tree. */
export interface AbwabTreeSnapshotVm {
  readonly sections: readonly AbwabTreeSectionDto[];
  readonly liveRoots: readonly AbwabNode[];
  readonly archivedRoots: readonly AbwabNode[];
  /** Item 19 — ROOT doors only, per section. Not reconcilable with `liveRoots.length` by
   * arithmetic: a live root can have `sectionId === null` (outside every section), so summing
   * this map's values can be strictly less than the total (abwab-tree.builder.ts). */
  readonly rootCountBySectionId: ReadonlyMap<number, number>;
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
  modal: 'modal',
} as const;

export interface AbwabQueryState {
  readonly section: number | null;
  readonly view: AbwabView;
  readonly archive: boolean;
  readonly door: number | null;
  readonly card: number | null;
  readonly q: string;
  readonly modal: AbwabModalState | null;
}

/** Every key fails closed to these when absent or invalid (plan-slice-b.md §4.4). */
export const ABWAB_QUERY_DEFAULTS: AbwabQueryState = {
  section: null,
  view: 'tree',
  archive: false,
  door: null,
  card: null,
  q: '',
  modal: null,
};
