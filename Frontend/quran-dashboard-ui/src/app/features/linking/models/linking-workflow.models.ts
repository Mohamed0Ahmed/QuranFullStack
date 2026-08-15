import { LinkingPreparedPreflightStatusDto } from '../../../core/api/generated/models/linking-prepared-preflight-status-dto';

export type LinkingWorkflowStep =
  | 'configure-source' | 'door' | 'preflighting' | 'ready'
  | 'submitting' | 'queued' | 'running' | 'finalizing'
  | 'succeeded' | 'failed' | 'cancelled';

export interface LinkingWorkflowState {
  origin: 'workspace' | 'source' | 'copy' | null;
  step: LinkingWorkflowStep;
  selectedDoorId: number | null;
  preparationKey: string | null;
  confirmationIdempotencyKey: string | null;
  prepared: LinkingPreparedPreflightStatusDto | null;
  errorMessage: string | null;
  operationGeneration: number;
}

export const INITIAL_LINKING_WORKFLOW: LinkingWorkflowState = {
  origin: null,
  step: 'configure-source',
  selectedDoorId: null,
  preparationKey: null,
  confirmationIdempotencyKey: null,
  prepared: null,
  errorMessage: null,
  operationGeneration: 0,
};

export const LINKING_WORKFLOW_NAVIGABLE_STEPS: readonly LinkingWorkflowStep[] = [
  'configure-source',
  'door',
  'preflighting',
  'ready',
];

export function isCopyPreflightNoOp(prepared: LinkingPreparedPreflightStatusDto): boolean {
  const totals = prepared.totals;
  return prepared.isBlocked !== true
    && totals !== null
    && totals.new === 0
    && totals.updated === 0
    && totals.removed === 0
    && totals.invalid === 0;
}

export function isStaleLinkingFailure(code: string | null): boolean {
  return code !== null && [
    'LINKING_DATA_STALE',
    'SOURCE_VIEW_STALE',
    'WORKSPACE_SOURCE_STALE',
    'PREFLIGHT_STALE',
  ].includes(code);
}
