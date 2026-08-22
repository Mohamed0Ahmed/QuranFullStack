import {
  AbwabRelationGroupKey,
  AbwabRelationKind,
  AbwabRelationVm,
  abwabRelationGroupKey,
} from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export type AbwabRelationsView = 'overview' | 'add';

const GROUP_LABELS: Readonly<Record<AbwabRelationGroupKey, string>> = {
  similarity: ABWAB_LABELS.relationGroupSimilarity,
  opposition: ABWAB_LABELS.relationGroupOpposition,
  'more-comprehensive': ABWAB_LABELS.relationGroupMoreComprehensive,
  'less-comprehensive': ABWAB_LABELS.relationGroupLessComprehensive,
};

const TYPE_LABELS: Readonly<Record<AbwabRelationKind, string>> = {
  similarity: ABWAB_LABELS.relationTypeSimilarity,
  opposition: ABWAB_LABELS.relationTypeOpposition,
  comprehensiveness: ABWAB_LABELS.relationTypeComprehensiveness,
};

const GROUP_DOT_KIND: Readonly<Record<AbwabRelationGroupKey, AbwabRelationKind>> = {
  similarity: 'similarity',
  opposition: 'opposition',
  'more-comprehensive': 'comprehensiveness',
  'less-comprehensive': 'comprehensiveness',
};

export const ABWAB_RELATIONS_MODAL_PRESENTATION = {
  typeOptions: ['similarity', 'opposition', 'comprehensiveness'] as const,
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
  startAddLabel: ABWAB_LABELS.relationStartAddButton,
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
  empty(canCreate: boolean): string {
    return canCreate ? ABWAB_LABELS.relationsEmpty : ABWAB_LABELS.relationsReadOnlyEmpty;
  },
  typeLabel(kind: AbwabRelationKind): string {
    return TYPE_LABELS[kind];
  },
  groupLabel(key: AbwabRelationGroupKey): string {
    return GROUP_LABELS[key];
  },
  groupDotKind(key: AbwabRelationGroupKey): AbwabRelationKind {
    return GROUP_DOT_KIND[key];
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
