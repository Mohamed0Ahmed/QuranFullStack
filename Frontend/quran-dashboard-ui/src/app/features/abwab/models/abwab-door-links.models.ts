import type { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import type { DoorLinkRecordSummaryDto } from '../../../core/api/generated/models/door-link-record-summary-dto';
import type { LinkingOperationSourceDraft } from '../../linking/models/linking-operation-draft.models';

export const ABWAB_DOOR_LINK_RECORD_PAGE_SIZE = 50;
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
export type AbwabDoorLinkCopyBatchStatus = 'pending' | 'preparing' | 'ready' | 'completed' | 'error';

export interface AbwabDoorLinkRecordPage {
  readonly page: number;
  readonly pageSize: number;
  readonly items: readonly DoorLinkRecordSummaryDto[];
}

export interface AbwabDoorLinkRecordsState {
  readonly status: AbwabDoorLinksLoadStatus;
  readonly pages: Readonly<Record<number, AbwabDoorLinkRecordPage>>;
  readonly requestedPage: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly errorMessage: string | null;
}

export interface AbwabDoorLinkAyahPage {
  readonly page: number;
  readonly pageSize: number;
  readonly items: readonly DoorLinkAyahDto[];
}

export interface AbwabDoorLinkExpandedState {
  readonly unitId: number;
  readonly isGrouped: boolean | null;
  readonly linkingDataRevision: number | null;
  readonly status: AbwabDoorLinksLoadStatus;
  readonly pages: Readonly<Record<number, AbwabDoorLinkAyahPage>>;
  readonly requestedPage: number;
  readonly pageSize: number;
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
  readonly scope: AbwabDoorLinkCopyScope | null;
  readonly targetDoorId: number | null;
  readonly batches: readonly AbwabDoorLinkCopyBatch[];
  readonly currentBatchNumber: number;
  readonly errors: readonly string[];
}

export interface AbwabDoorLinksState {
  readonly openDoorId: number | null;
  readonly doorVersion: number | null;
  readonly records: AbwabDoorLinkRecordsState;
  readonly expanded: AbwabDoorLinkExpandedState | null;
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
