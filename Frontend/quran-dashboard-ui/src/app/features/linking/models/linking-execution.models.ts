import { LinkingConfirmationJobStatusDto } from '../../../core/api/generated/models/linking-confirmation-job-status-dto';
import { LinkingConfirmationSubmissionResponse } from '../../../core/api/generated/models/linking-confirmation-submission-response';
import { LinkingDurableConfirmationOutcomeDto } from '../../../core/api/generated/models/linking-durable-confirmation-outcome-dto';

export type LinkingConfirmationJobStatus = LinkingConfirmationJobStatusDto;
export type LinkingConfirmationSubmission = LinkingConfirmationSubmissionResponse;
export type LinkingDurableConfirmationOutcome = LinkingDurableConfirmationOutcomeDto;

export function isConfirmationJobTerminal(status: LinkingConfirmationJobStatus): boolean {
  return ['succeeded', 'stale', 'failed', 'cancelled'].includes(status.status.toLowerCase());
}
