import { Injectable, signal } from '@angular/core';

import {
  LinkingAddFeedback,
  LinkingWorkspaceAddStatus,
} from '../models/linking-workspace-add.models';

const FEEDBACK_DURATION_MS = 3000;

@Injectable({ providedIn: 'root' })
export class LinkingAddFeedbackStore {
  private readonly feedbackSignal = signal<LinkingAddFeedback | null>(null);
  private feedbackId = 0;
  private dismissTimer: ReturnType<typeof setTimeout> | null = null;

  readonly feedback = this.feedbackSignal.asReadonly();

  show(status: LinkingWorkspaceAddStatus, label: string): void {
    if (this.dismissTimer !== null) {
      clearTimeout(this.dismissTimer);
    }

    this.feedbackId += 1;
    this.feedbackSignal.set({
      id: this.feedbackId,
      status,
      message: status === 'added'
        ? `تمت إضافة «${label}» إلى مساحة الربط.`
        : `«${label}» موجود بالفعل في مساحة الربط.`,
    });
    this.dismissTimer = setTimeout(() => {
      this.feedbackSignal.set(null);
      this.dismissTimer = null;
    }, FEEDBACK_DURATION_MS);
  }
}
