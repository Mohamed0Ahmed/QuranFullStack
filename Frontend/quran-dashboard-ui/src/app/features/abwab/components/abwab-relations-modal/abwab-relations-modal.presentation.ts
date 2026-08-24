import {
  AbwabRelationGroupKey,
  AbwabRelationDirectionKind,
  AbwabRelationKind,
  AbwabRelationVm,
  abwabRelationGroupKey,
} from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export type AbwabRelationsView = 'overview' | 'add';

const TYPE_LABELS: Readonly<Record<AbwabRelationKind, string>> = {
  similarity: ABWAB_LABELS.relationTypeSimilarity,
  opposition: ABWAB_LABELS.relationTypeOpposition,
  comprehensiveness: ABWAB_LABELS.relationTypeComprehensiveness,
};

export interface AbwabRelationsOverviewTab {
  readonly key: AbwabRelationGroupKey;
  readonly label: string;
  readonly emptyMessage: string;
}

export interface AbwabRelationAddDraft {
  readonly kind: AbwabRelationKind;
  readonly direction: AbwabRelationDirectionKind | null;
}

const ADD_DRAFT_BY_GROUP: Readonly<Record<AbwabRelationGroupKey, AbwabRelationAddDraft>> = {
  similarity: { kind: 'similarity', direction: null },
  opposition: { kind: 'opposition', direction: null },
  'more-comprehensive': { kind: 'comprehensiveness', direction: 'anchor-less' },
  'less-comprehensive': { kind: 'comprehensiveness', direction: 'anchor-more' },
};

export function abwabRelationAddDraftForGroup(groupKey: AbwabRelationGroupKey): AbwabRelationAddDraft {
  return ADD_DRAFT_BY_GROUP[groupKey];
}

export const ABWAB_RELATIONS_MODAL_PRESENTATION = {
  typeOptions: ['similarity', 'opposition', 'comprehensiveness'] as const,
  overviewTabs: [
    {
      key: 'similarity',
      label: 'أبواب متشابهة',
      emptyMessage: 'لا توجد أبواب متشابهة مرتبطة بهذا الباب بعد',
    },
    {
      key: 'opposition',
      label: 'أبواب متضادة',
      emptyMessage: 'لا توجد أبواب متضادة مرتبطة بهذا الباب بعد',
    },
    {
      key: 'more-comprehensive',
      label: 'أبواب أكثر شمولية',
      emptyMessage: 'لا توجد أبواب أكثر شمولية مرتبطة بهذا الباب بعد',
    },
    {
      key: 'less-comprehensive',
      label: 'أبواب أقل شمولية',
      emptyMessage: 'لا توجد أبواب أقل شمولية مرتبطة بهذا الباب بعد',
    },
  ] as const satisfies readonly AbwabRelationsOverviewTab[],
  overviewTabsAriaLabel: 'أنواع علاقات الباب',
  addTitle: ABWAB_LABELS.relationAddTitle,
  loadingLabel: ABWAB_LABELS.relationsLoading,
  retryLabel: ABWAB_LABELS.retryButton,
  directionLabel: ABWAB_LABELS.relationDirectionLabel,
  typeTabsAriaLabel: ABWAB_LABELS.relationTypeTabsAriaLabel,
  alreadyLinkedLabel: ABWAB_LABELS.relationAlreadyLinked,
  pickerEmptyLabel: ABWAB_LABELS.relationPickerEmptyDoors,
  noneSelectedLabel: ABWAB_LABELS.relationNoneSelected,
  bulkAnchorHint: ABWAB_LABELS.relationsBulkAnchorHint,
  closeLabel: ABWAB_LABELS.relationsCloseButton,
  startAddLabel: 'إضافة علاقة',
  backLabel: ABWAB_LABELS.relationBackButton,
  deleteConfirmTitle: ABWAB_LABELS.relationDeleteConfirmTitle,
  deleteConfirmLabel: ABWAB_LABELS.deleteConfirmButton,
  cancelLabel: ABWAB_LABELS.cancelButton,
  deleteConfirmSides: ABWAB_LABELS.relationDeleteConfirmSides,
  description(view: AbwabRelationsView, canDelete: boolean): string {
    if (view === 'add') {
      return ABWAB_LABELS.relationAddDescription;
    }
    return canDelete ? ABWAB_LABELS.relationsModalDescription : ABWAB_LABELS.relationsReadOnlyDescription;
  },
  typeLabel(kind: AbwabRelationKind): string {
    return TYPE_LABELS[kind];
  },
  deleteConfirmBody(anchorDoorName: string, relation: AbwabRelationVm): string {
    return ABWAB_LABELS.relationDeleteConfirmBody(
      anchorDoorName,
      relation.otherDoorName,
      abwabRelationGroupKey(relation),
    );
  },
  deleteAriaLabel(doorName: string): string {
    return ABWAB_LABELS.relationDeleteAriaLabel(doorName);
  },
  revealAriaLabel(doorName: string): string {
    return ABWAB_LABELS.doorRevealAriaLabel(doorName);
  },
};
