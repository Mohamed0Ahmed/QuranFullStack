import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import {
  PRISTINE_SUBMISSION,
  VALIDATION_MESSAGE_RESOLVER,
  ValidationMessageResolver,
  beginSubmission,
  rejectSubmission,
} from './form-conventions';

describe('form conventions', () => {
  it('PRISTINE_SUBMISSION is an editing baseline with no errors', () => {
    expect(PRISTINE_SUBMISSION.status).toBe('editing');
    expect(PRISTINE_SUBMISSION.formError).toBeNull();
    expect(PRISTINE_SUBMISSION.fieldErrors).toEqual({});
  });

  it('beginSubmission clears prior errors and marks the form submitting', () => {
    const rejected = rejectSubmission({ name: [{ code: 'required' }] }, 'راجع الحقول');

    const submitting = beginSubmission(rejected);

    expect(submitting.status).toBe('submitting');
    expect(submitting.fieldErrors).toEqual({});
    expect(submitting.formError).toBeNull();
  });

  it('rejectSubmission records field and form errors under a rejected status', () => {
    const state = rejectSubmission({ email: [{ code: 'format' }] }, 'تعذر الحفظ');

    expect(state.status).toBe('rejected');
    expect(state.fieldErrors['email']).toEqual([{ code: 'format' }]);
    expect(state.formError).toBe('تعذر الحفظ');
  });

  it('the validation message resolver token is injectable and maps a field error to a message', () => {
    const resolver: ValidationMessageResolver = (error) =>
      error.code === 'required' ? 'هذا الحقل مطلوب' : 'قيمة غير صالحة';

    TestBed.configureTestingModule({
      providers: [{ provide: VALIDATION_MESSAGE_RESOLVER, useValue: resolver }],
    });

    const resolve = TestBed.inject(VALIDATION_MESSAGE_RESOLVER);
    expect(resolve({ code: 'required' })).toBe('هذا الحقل مطلوب');
    expect(resolve({ code: 'format' })).toBe('قيمة غير صالحة');
  });
});
