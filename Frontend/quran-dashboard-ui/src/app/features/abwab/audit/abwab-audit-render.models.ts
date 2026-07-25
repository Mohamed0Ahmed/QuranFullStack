// §6.3 core audit render payloads published by 029 (audit-render-contract.md). These are pure view
// models for the presentational components in this folder — 029 does not build the main audit page,
// pagination, or fetch real audit records (033 owns that read model); it only defines and renders
// the SHAPE of these five payloads over whatever changeset a caller supplies.
export const EMPTY_FIELD_VALUE_LABEL = 'غير محدد';
export const DORMANT_LABEL = 'خامل';

export function formatFieldValue(value: string | null | undefined): string {
  return value != null && value.trim().length > 0 ? value : EMPTY_FIELD_VALUE_LABEL;
}

export function formatOrderValue(value: number | null | undefined): string {
  return value == null ? EMPTY_FIELD_VALUE_LABEL : String(value);
}

export interface FieldDiff {
  readonly key: string;
  readonly label: string;
  readonly before: string | null;
  readonly after: string | null;
  readonly changed: boolean;
}

export interface CategoryCreateRenderPayload {
  readonly name: string;
  readonly representativeQuranExcerpt: string | null;
  readonly description: string | null;
  readonly aliases: readonly string[];
  readonly sectionName: string;
  readonly parentPath: readonly string[];
  readonly siblingOrder: number | null;
  readonly sectionOrder: number | null;
  readonly globalOrder: number | null;
}

export interface CategoryEditRenderPayload {
  readonly categoryId: string;
  readonly categoryName: string;
  readonly fields: readonly FieldDiff[];
}

export interface BulkMoveRootEntry {
  readonly categoryId: string;
  readonly name: string;
  readonly beforeSectionName: string;
  readonly beforePath: readonly string[];
  readonly beforeOrder: number | null;
  readonly afterSectionName: string;
  readonly afterPath: readonly string[];
  readonly afterOrder: number | null;
  readonly movedDescendantCount: number;
  readonly movedDescendantNames: readonly string[];
}

export type CategoryOrderScopeLabel = 'الترتيب بين الإخوة' | 'ترتيب الأبواب الرئيسية داخل القسم' | 'الترتيب العام للأبواب الرئيسية';

export interface SiblingOrderSideEffectEntry {
  readonly categoryName: string;
  readonly beforeOrder: number | null;
  readonly afterOrder: number | null;
}

export interface SiblingOrderSideEffectGroup {
  readonly parentLabel: string;
  readonly orderScopeLabel: CategoryOrderScopeLabel;
  readonly entries: readonly SiblingOrderSideEffectEntry[];
}

export interface BulkMoveRenderPayload {
  readonly changeSetId: string;
  readonly selectedRootCount: number;
  readonly roots: readonly BulkMoveRootEntry[];
  readonly siblingOrderSideEffects: readonly SiblingOrderSideEffectGroup[];
}

export interface DormantDependentCount {
  readonly label: string;
  readonly count: number;
}

export interface SubtreeDeleteRenderPayload {
  readonly changeSetId: string;
  readonly deletionOperationId: string;
  readonly rootName: string;
  readonly historicalPath: readonly string[];
  readonly affectedCategoryCount: number;
  readonly dormantDependentCounts: readonly DormantDependentCount[];
  readonly isRestored: boolean;
  readonly currentPath: readonly string[] | null;
}

export type ProtectionEffectState = 'protected' | 'unprotected';

export interface ManualProtectionEffectChange {
  readonly categoryName: string;
  readonly isDirect: boolean;
  readonly before: ProtectionEffectState;
  readonly after: ProtectionEffectState;
}

export type RelationshipAction = 'added' | 'edited' | 'deleted' | 'restored';

export interface RelationshipEndpointRenderView {
  readonly categoryId: string;
  readonly name: string;
  readonly sectionName: string | null;
  readonly historicalPath: readonly string[];
  readonly currentName: string | null;
  readonly currentPath: readonly string[] | null;
  readonly isCurrentlyDeleted: boolean;
}

export interface RelationshipStateRenderView {
  readonly typeLabel: string;
  readonly isDirectional: boolean;
  readonly from: RelationshipEndpointRenderView;
  readonly to: RelationshipEndpointRenderView;
}

export interface RelationshipRenderPayload {
  readonly changeSetId: string;
  readonly categoryRelationshipId: string;
  readonly action: RelationshipAction;
  readonly before: RelationshipStateRenderView | null;
  readonly after: RelationshipStateRenderView | null;
}

export interface ManualProtectionRenderPayload {
  readonly changeSetId: string;
  readonly targetCategoryName: string;
  readonly protectionTypeLabel: string;
  readonly scopeLabel: string;
  readonly actorSubject: string;
  readonly actedAtUtc: string;
  readonly before: ProtectionEffectState;
  readonly after: ProtectionEffectState;
  readonly effectChanges: readonly ManualProtectionEffectChange[];
}
