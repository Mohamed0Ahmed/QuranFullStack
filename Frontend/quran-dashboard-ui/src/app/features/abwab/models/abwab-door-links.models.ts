import type { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import type { DoorLinkRecordSummaryDto } from '../../../core/api/generated/models/door-link-record-summary-dto';
import type { LinkingOperationSourceDraft } from '../../linking/models/linking-operation-draft.models';

export const ABWAB_DOOR_LINK_AYAH_PAGE_SIZE = 50;
export const ABWAB_DOOR_LINK_COPY_BATCH_SIZE = 100;

export type AbwabDoorLinksLoadStatus = 'idle' | 'loading' | 'refreshing' | 'ready' | 'empty' | 'error';
export type AbwabDoorLinksWriteStatus = 'idle' | 'writing' | 'error';
export type AbwabDoorLinkEditStatus =
  | 'idle'
  | 'preparing'
  | 'ready'
  | 'saving'
  | 'load-error'
  | 'save-error';
export type AbwabDoorLinkSelectionMode = 'only' | 'all-except';
export type AbwabDoorLinkCopyScope = 'selected' | 'all';
export type AbwabDoorLinkCopyStatus =
  | 'choosing'
  | 'enumerating'
  | 'preparing'
  | 'running'
  | 'stopped';
export type AbwabDoorLinkCopyBatchStatus = 'pending' | 'preparing' | 'running' | 'completed' | 'error';

export interface AbwabDoorLinkRecordView {
  readonly summary: DoorLinkRecordSummaryDto;
  readonly ayahs: readonly DoorLinkAyahDto[];
}

export interface AbwabDoorLinkRecordsState {
  readonly status: AbwabDoorLinksLoadStatus;
  readonly items: readonly AbwabDoorLinkRecordView[];
  readonly linkingDataRevision: number | null;
  readonly totalCount: number;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinkSelectionState {
  readonly mode: AbwabDoorLinkSelectionMode;
  readonly unitIds: readonly number[];
}

export interface AbwabDoorLinkEditState {
  readonly unitId: number | null;
  readonly expectedDoorVersion: number | null;
  readonly ayahs: readonly DoorLinkAyahDto[];
  readonly status: AbwabDoorLinkEditStatus;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinkDeleteState {
  readonly confirmationOpen: boolean;
  readonly status: AbwabDoorLinksWriteStatus;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinkCopyBatch {
  readonly batchNumber: number;
  readonly unitIds: readonly number[];
  readonly sources: readonly LinkingOperationSourceDraft[];
  readonly status: AbwabDoorLinkCopyBatchStatus;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinkCopyState {
  readonly open: boolean;
  readonly status: AbwabDoorLinkCopyStatus;
  readonly scope: AbwabDoorLinkCopyScope | null;
  readonly sourceDoorId: number | null;
  readonly expectedSourceDoorVersion: number | null;
  readonly expectedLinkingDataRevision: number | null;
  readonly sourceSelection: AbwabDoorLinkSelectionState | null;
  readonly targetDoorId: number | null;
  readonly batches: readonly AbwabDoorLinkCopyBatch[];
  readonly currentBatchNumber: number;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinksState {
  readonly openDoorId: number | null;
  readonly doorVersion: number | null;
  readonly records: AbwabDoorLinkRecordsState;
  readonly selection: AbwabDoorLinkSelectionState;
  readonly edit: AbwabDoorLinkEditState;
  readonly deletion: AbwabDoorLinkDeleteState;
  readonly copy: AbwabDoorLinkCopyState;
  readonly staleMessage: string | null;
  readonly noticeMessage: string | null;
}

export interface AbwabDoorLinkCopyRecord {
  readonly unitId: number;
  readonly isGrouped: boolean;
  readonly linkingDataRevision: number;
  readonly ayahs: readonly DoorLinkAyahDto[];
}

export const EMPTY_ABWAB_DOOR_LINK_SELECTION: AbwabDoorLinkSelectionState = {
  mode: 'only',
  unitIds: [],
};
