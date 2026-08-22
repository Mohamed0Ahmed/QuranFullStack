import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabReorderScope } from '../../../core/api/generated/models/abwab-reorder-scope';
import { AbwabRelationType } from '../../../core/api/generated/models/abwab-relation-type';
import { AbwabRelationDirection } from '../../../core/api/generated/models/abwab-relation-direction';

export type AbwabView = 'tree' | 'cards';

export type AbwabModalKind = 'create' | 'child' | 'edit' | 'move' | 'sections' | 'relations' | 'inclusions';

export interface AbwabModalState {
  readonly kind: AbwabModalKind;
  readonly closed: boolean;
  readonly subjectDoorId: number | null;
}

export type AbwabOrderScope = 'global' | 'section';

export const ABWAB_ORDER_SCOPE_TO_WIRE: Readonly<Record<AbwabOrderScope, AbwabReorderScope>> = {
  section: 1,
  global: 2,
};

export type AbwabRelationKind = 'similarity' | 'opposition' | 'comprehensiveness';

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

export interface AbwabRelationVm {
  readonly id: number;
  readonly otherDoorId: number;
  readonly otherDoorName: string;
  readonly kind: AbwabRelationKind;
  readonly direction: AbwabRelationDirectionKind | null;
}

export type AbwabRelationGroupKey = 'similarity' | 'opposition' | 'more-comprehensive' | 'less-comprehensive';

export interface AbwabRelationGroupVm {
  readonly key: AbwabRelationGroupKey;
  readonly relations: readonly AbwabRelationVm[];
}

const ABWAB_RELATION_GROUP_ORDER: readonly AbwabRelationGroupKey[] = [
  'similarity',
  'opposition',
  'more-comprehensive',
  'less-comprehensive',
];

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
  'inclusions',
]);

export function isAbwabModalKind(value: unknown): value is AbwabModalKind {
  return typeof value === 'string' && ABWAB_MODAL_KINDS.has(value);
}

const ABWAB_DOOR_DEPENDENT_MODAL_KINDS: ReadonlySet<AbwabModalKind> = new Set<AbwabModalKind>([
  'child',
  'edit',
  'move',
  'relations',
  'inclusions',
]);

export function isDoorDependentAbwabModalKind(kind: AbwabModalKind): boolean {
  return ABWAB_DOOR_DEPENDENT_MODAL_KINDS.has(kind);
}

export interface AbwabNode {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly representativeAyahText: string | null;
  readonly aliases: readonly string[];
  readonly sectionId: number;
  readonly sectionRetired: boolean;
  readonly parentId: number | null;
  readonly orderValue: number;
  readonly globalOrderValue: number | null;
  readonly version: number;
  readonly isArchived: boolean;
  readonly depth: number;
  readonly liveChildCount: number;
  readonly liveDescendantCount: number;
  readonly maxRelativeDepth: number;
  readonly relationCount: number;
  readonly linkCount: number;
  readonly selectedWordCount: number;
  readonly inclusionSourceCount: number;
  readonly inclusionConsumerCount: number;
  readonly children: readonly AbwabNode[];
}

export interface AbwabTreeSnapshotVm {
  readonly sections: readonly AbwabTreeSectionDto[];
  readonly liveRoots: readonly AbwabNode[];
  readonly archivedRoots: readonly AbwabNode[];
  readonly rootCountBySectionId: ReadonlyMap<number, number>;
  readonly byId: ReadonlyMap<number, AbwabNode>;
  readonly version: string | null;
}

export interface AbwabMoveDestination {
  readonly targetParentId: number | null;
  readonly targetSectionId: number;
}

export const ABWAB_QUERY_KEYS = {
  section: 'section',
  view: 'view',
  archive: 'archive',
  door: 'door',
  card: 'card',
  q: 'q',
  hideUnrelatedRoots: 'hideUnrelatedRoots',
  modal: 'modal',
} as const;

export interface AbwabQueryState {
  readonly section: number | null;
  readonly view: AbwabView;
  readonly archive: boolean;
  readonly door: number | null;
  readonly card: number | null;
  readonly q: string;
  readonly hideUnrelatedRoots: boolean;
  readonly modal: AbwabModalState | null;
}

export const ABWAB_QUERY_DEFAULTS: AbwabQueryState = {
  section: null,
  view: 'tree',
  archive: false,
  door: null,
  card: null,
  q: '',
  hideUnrelatedRoots: true,
  modal: null,
};
