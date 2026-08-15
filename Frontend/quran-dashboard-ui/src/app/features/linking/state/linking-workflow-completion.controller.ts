import { Injectable, inject } from '@angular/core';

import { LinkingCopyCallbacks } from '../models/linking-operation-draft.models';
import { LinkingWorkflowState } from '../models/linking-workflow.models';
import { LinkingExecutionStore } from './linking-execution.store';
import { LinkingWorkspaceStore } from './linking-workspace.store';

@Injectable({ providedIn: 'root' })
export class LinkingWorkflowCompletionController {
  private readonly execution = inject(LinkingExecutionStore);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private copyCallbacks: LinkingCopyCallbacks | null = null;
  private acknowledgementTask: Promise<void> | null = null;

  setCopyCallbacks(callbacks: LinkingCopyCallbacks): void {
    this.copyCallbacks = callbacks;
  }

  clearCopyCallbacks(): void {
    this.copyCallbacks = null;
  }

  executionSucceeded(idempotencyKey: string | null): boolean {
    const execution = this.execution.state();
    return idempotencyKey !== null
      && execution.idempotencyKey === idempotencyKey
      && execution.status === 'succeeded';
  }

  executionInFlight(idempotencyKey: string | null): boolean {
    const execution = this.execution.state();
    return idempotencyKey !== null
      && execution.idempotencyKey === idempotencyKey
      && ['submitting', 'polling'].includes(execution.status);
  }

  acknowledge(origin: LinkingWorkflowState['origin'], finalize: () => void): Promise<void> {
    if (this.acknowledgementTask !== null) {
      return this.acknowledgementTask;
    }
    const acknowledged = origin === 'copy' ? this.copyCallbacks?.acknowledged ?? null : null;
    this.copyCallbacks = null;
    this.acknowledgementTask = this.completeAcknowledgement(origin, acknowledged, finalize);
    return this.acknowledgementTask;
  }

  stopCopy(message: string): void {
    const stopped = this.copyCallbacks?.stopped ?? null;
    this.copyCallbacks = null;
    stopped?.(message);
  }

  private async completeAcknowledgement(
    origin: LinkingWorkflowState['origin'],
    acknowledged: (() => void) | null,
    finalize: () => void,
  ): Promise<void> {
    try {
      await this.execution.acknowledge();
    } finally {
      if (origin === 'workspace') {
        this.workspace.clearCheckedSources();
      }
      finalize();
      this.acknowledgementTask = null;
      acknowledged?.();
    }
  }
}
