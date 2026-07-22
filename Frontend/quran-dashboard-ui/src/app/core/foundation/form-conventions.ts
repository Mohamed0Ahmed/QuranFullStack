import { injectionToken } from './injection-token';

export interface FieldError {
  readonly code: string;
  readonly params?: Readonly<Record<string, unknown>>;
}

export type ValidationMessageResolver = (error: FieldError) => string;

export const VALIDATION_MESSAGE_RESOLVER = injectionToken<ValidationMessageResolver>(
  'qd.forms.validation-message-resolver',
);

export type SubmissionStatus = 'editing' | 'submitting' | 'submitted' | 'rejected';

export interface SubmissionState {
  readonly status: SubmissionStatus;
  readonly fieldErrors: Readonly<Record<string, readonly FieldError[]>>;
  readonly formError: string | null;
}

export const PRISTINE_SUBMISSION: SubmissionState = {
  status: 'editing',
  fieldErrors: {},
  formError: null,
};

export function beginSubmission(_previous: SubmissionState): SubmissionState {
  return { status: 'submitting', fieldErrors: {}, formError: null };
}

export function rejectSubmission(
  fieldErrors: Readonly<Record<string, readonly FieldError[]>>,
  formError: string | null = null,
): SubmissionState {
  return { status: 'rejected', fieldErrors, formError };
}
