import { Injectable, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { LinkingExecutionApi } from '../data-access/linking-execution.api';
import { LinkingJobStatusApi } from '../data-access/linking-job-status.api';
import {
  LinkingConfirmationJobStatus,
  LinkingDurableConfirmationOutcome,
  isConfirmationJobTerminal,
} from '../models/linking-execution.models';
import { LinkingRecoveryStore } from './linking-recovery.store';
import { LinkingStatusPollRunner } from './linking-status-poll.runner';
import { LinkingLifecycleError } from '../models/linking-revision.models';

export interface LinkingExecutionState {
  status: 'idle' | 'submitting' | 'polling' | 'succeeded' | 'failed';
  job: LinkingConfirmationJobStatus | null;
  outcome: LinkingDurableConfirmationOutcome | null;
  idempotencyKey: string | null;
  errorMessage: string | null;
  failureCode: string | null;
  generation: number;
}

const INITIAL_STATE: LinkingExecutionState = {
  status: 'idle',
  job: null,
  outcome: null,
  idempotencyKey: null,
  errorMessage: null,
  failureCode: null,
  generation: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingExecutionStore {
  private readonly currentUser = inject(CurrentUserStore);
  private readonly api = inject(LinkingExecutionApi);
  private readonly jobs = inject(LinkingJobStatusApi);
  private readonly recovery = inject(LinkingRecoveryStore);
  private readonly poller = inject(LinkingStatusPollRunner);
  private readonly stateSignal = signal<LinkingExecutionState>(INITIAL_STATE);
  private actorSub: string | null = null;

  readonly state = this.stateSignal.asReadonly();

  constructor() {
    effect(() => {
      const actorSub = this.currentUser.currentUser()?.sub ?? null;
      if (actorSub !== this.actorSub) {
        this.poller.cancel(this.pollKey());
        this.actorSub = actorSub;
        this.stateSignal.set({ ...INITIAL_STATE, generation: this.stateSignal().generation + 1 });
      }
    });
  }

  async execute(
    preparationKey: string,
    preflightId: string,
    preflightToken: string,
    idempotencyKey: string,
  ): Promise<void> {
    const actorSub = this.requireActor();
    const generation = this.stateSignal().generation + 1;
    this.poller.cancel(this.pollKey());
    this.stateSignal.set({
      ...INITIAL_STATE,
      status: 'submitting',
      idempotencyKey,
      generation,
    });
    try {
      await this.recovery.appendConfirmation(
        actorSub,
        preparationKey,
        preflightId,
        preflightToken,
        idempotencyKey,
      );
      const submission = await firstValueFrom(
        this.api.createJob(preflightId, preflightToken, idempotencyKey),
      );
      if (submission.durableOutcome !== null) {
        await this.publishOutcome(generation, submission.durableOutcome);
        return;
      }
      if (submission.job === null) {
        throw new Error('لم يُرجع الخادم مهمة تنفيذ أو نتيجة محفوظة.');
      }
      await this.recovery.setJobId(actorSub, idempotencyKey, submission.job.jobId);
      this.publishJob(generation, submission.job);
    } catch (error: unknown) {
      this.fail(generation, error);
    }
  }

  async cancel(): Promise<void> {
    const state = this.stateSignal();
    if (state.job === null) {
      return;
    }
    this.poller.cancel(this.pollKey());
    try {
      this.publishJob(state.generation, await firstValueFrom(this.jobs.cancel(state.job.jobId)));
    } catch (error: unknown) {
      this.fail(state.generation, error);
    }
  }

  dismiss(): void {
    this.poller.cancel(this.pollKey());
    this.stateSignal.set({ ...INITIAL_STATE, generation: this.stateSignal().generation + 1 });
  }

  async acknowledge(): Promise<void> {
    const state = this.stateSignal();
    if (state.idempotencyKey !== null && (state.outcome !== null || state.job !== null && isConfirmationJobTerminal(state.job))) {
      await this.recovery.acknowledge(this.requireActor(), 'confirmation', state.idempotencyKey);
    }
  }

  private publishJob(
    generation: number,
    job: LinkingConfirmationJobStatus,
    beginPolling = true,
  ): void {
    if (this.stateSignal().generation !== generation) {
      return;
    }
    const terminal = isConfirmationJobTerminal(job);
    this.stateSignal.update((state) => ({
      ...state,
      status: terminal ? (job.status.toLowerCase() === 'succeeded' ? 'succeeded' : 'failed') : 'polling',
      job,
      errorMessage: job.failureCode,
      failureCode: job.failureCode,
    }));
    if (terminal) {
      void this.recovery.markTerminal(this.requireActor(), 'confirmation', this.requireIdempotencyKey());
      this.poller.cancel(this.pollKey());
    } else if (beginPolling) {
      this.startPolling(generation, job.jobId);
    }
  }

  private startPolling(generation: number, jobId: string): void {
    this.poller.start(
      this.pollKey(),
      generation,
      () => this.jobs.get(jobId),
      isConfirmationJobTerminal,
      (job) => job.pollAfterMs,
      (job) => this.publishJob(generation, job, false),
      (error) => this.fail(generation, error),
    );
  }

  private async publishOutcome(
    generation: number,
    outcome: LinkingDurableConfirmationOutcome,
  ): Promise<void> {
    if (this.stateSignal().generation !== generation) {
      return;
    }
    this.stateSignal.update((state) => ({ ...state, status: 'succeeded', outcome }));
    await this.recovery.markTerminal(this.requireActor(), 'confirmation', outcome.idempotencyKey);
  }

  private fail(generation: number, error: unknown): void {
    if (this.stateSignal().generation === generation) {
      this.stateSignal.update((state) => ({
        ...state,
        status: 'failed',
        errorMessage: error instanceof Error ? error.message : 'تعذر تنفيذ الربط.',
        failureCode: error instanceof LinkingLifecycleError ? error.code : null,
      }));
    }
  }

  private requireActor(): string {
    const actorSub = this.currentUser.currentUser()?.sub;
    if (actorSub === undefined) {
      throw new Error('يجب تسجيل الدخول قبل تنفيذ الربط.');
    }
    return actorSub;
  }

  private requireIdempotencyKey(): string {
    const key = this.stateSignal().idempotencyKey;
    if (key === null) {
      throw new Error('مفتاح تنفيذ الربط غير متاح.');
    }
    return key;
  }

  private pollKey(): string {
    return 'linking-confirmation-job';
  }
}
