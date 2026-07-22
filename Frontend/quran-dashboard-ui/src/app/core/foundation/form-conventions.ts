import { injectionToken } from './injection-token';

// Form CONVENTIONS only — deliberately free of `@angular/forms` (Forms arrives with the first real form in
// a later Kit). These are the framework-agnostic contracts a Reactive form will later adopt: how a field
// error is shaped, how it is turned into a user-facing message, and the submission lifecycle a form store
// exposes. Keeping them here lets the substrate settle the shape before the Forms package is installed.

export interface FieldError {
  readonly code: string;
  readonly params?: Readonly<Record<string, unknown>>;
}

export type ValidationMessageResolver = (error: FieldError) => string;

// Later forms inject this to localize a field error; the substrate ships only the contract, not a resolver.
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
