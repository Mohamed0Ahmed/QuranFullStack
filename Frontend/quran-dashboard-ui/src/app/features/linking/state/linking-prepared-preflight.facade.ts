import { Injectable, Signal, WritableSignal, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { LinkingPreparedPreflightApi } from '../data-access/linking-prepared-preflight.api';
import {
  LinkingPreparedPreflightRequest,
  LinkingPreparedPreflightStatus,
  isPreparedPreflightReady,
  isPreparedPreflightTerminal,
} from '../models/linking-prepared-preflight.models';
import { LinkingRecoveryStore } from './linking-recovery.store';
import { LinkingStatusPollRunner } from './linking-status-poll.runner';
import { LinkingLifecycleError } from '../models/linking-revision.models';

export interface LinkingPreparedPreflightState {
  status: 'idle' | 'submitting' | 'polling' | 'ready' | 'error';
  resource: LinkingPreparedPreflightStatus | null;
  errorMessage: string | null;
  failureCode: string | null;
  generation: number;
}

const INITIAL_STATE: LinkingPreparedPreflightState = {
  status: 'idle',
  resource: null,
  errorMessage: null,
  failureCode: null,
  generation: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingPreparedPreflightFacade {
  private readonly authSession = inject(AuthSessionStore);
  private readonly api = inject(LinkingPreparedPreflightApi);
  private readonly recovery = inject(LinkingRecoveryStore);
  private readonly poller = inject(LinkingStatusPollRunner);
  private readonly states = new Map<string, WritableSignal<LinkingPreparedPreflightState>>();
  private actorSub: string | null = null;

  constructor() {
    effect(() => {
      const actorSub = this.authSession.subject();
      if (actorSub !== this.actorSub) {
        this.states.forEach((_state, key) => this.poller.cancel(this.pollKey(key)));
        this.states.clear();
        this.actorSub = actorSub;
      }
    });
  }

  stateFor(preparationKey: string): Signal<LinkingPreparedPreflightState> {
    return this.stateSignal(preparationKey).asReadonly();
  }

  async create(request: LinkingPreparedPreflightRequest): Promise<LinkingPreparedPreflightStatus | null> {
    const actorSub = this.requireActor();
    const preparationKey = requirePreparationKey(request);
    const state = this.stateSignal(preparationKey);
    const generation = state().generation + 1;
    state.set({ status: 'submitting', resource: null, errorMessage: null, failureCode: null, generation });
    try {
      await this.recovery.appendPreparation(actorSub, request);
      const resource = await firstValueFrom(this.api.create(request));
      await this.recovery.setPreflightId(actorSub, preparationKey, resource.preflightId);
      this.publish(preparationKey, generation, resource);
      return resource;
    } catch (error: unknown) {
      this.fail(preparationKey, generation, error);
      return null;
    }
  }

  open(preparationKey: string, preflightId: string): void {
    const state = this.stateSignal(preparationKey);
    const generation = state().generation + 1;
    state.set({ status: 'polling', resource: null, errorMessage: null, failureCode: null, generation });
    this.startPolling(preparationKey, generation, preflightId);
  }

  async cancel(preparationKey: string): Promise<void> {
    const state = this.stateSignal(preparationKey);
    const resource = state().resource;
    if (resource === null) {
      return;
    }
    this.poller.cancel(this.pollKey(preparationKey));
    try {
      const cancelled = await firstValueFrom(this.api.cancel(resource.preflightId));
      this.publish(preparationKey, state().generation, cancelled);
    } catch (error: unknown) {
      this.fail(preparationKey, state().generation, error);
    }
  }

  dismiss(preparationKey: string): void {
    this.poller.cancel(this.pollKey(preparationKey));
    this.states.delete(preparationKey);
  }

  async acknowledge(preparationKey: string): Promise<void> {
    const actorSub = this.requireActor();
    const resource = this.stateSignal(preparationKey)().resource;
    if (
      resource !== null &&
      isPreparedPreflightTerminal(resource) &&
      !isPreparedPreflightReady(resource)
    ) {
      await this.recovery.acknowledge(actorSub, 'preparation', preparationKey);
    }
  }

  private publish(
    preparationKey: string,
    generation: number,
    resource: LinkingPreparedPreflightStatus,
    beginPolling = true,
  ): void {
    const state = this.stateSignal(preparationKey);
    if (state().generation !== generation) {
      return;
    }
    const terminal = isPreparedPreflightTerminal(resource);
    state.set({
      status: terminal ? 'ready' : 'polling',
      resource,
      errorMessage: null,
      failureCode: resource.failureCode,
      generation,
    });
    if (terminal) {
      if (!isPreparedPreflightReady(resource)) {
        void this.recovery.markTerminal(this.requireActor(), 'preparation', preparationKey);
      }
      this.poller.cancel(this.pollKey(preparationKey));
    } else if (beginPolling) {
      this.startPolling(preparationKey, generation, resource.preflightId);
    }
  }

  private startPolling(preparationKey: string, generation: number, preflightId: string): void {
    this.poller.start(
      this.pollKey(preparationKey),
      generation,
      () => this.api.get(preflightId),
      isPreparedPreflightTerminal,
      (resource) => resource.pollAfterMs,
      (resource) => this.publish(preparationKey, generation, resource, false),
      (error) => this.fail(preparationKey, generation, error),
    );
  }

  private fail(preparationKey: string, generation: number, error: unknown): void {
    const state = this.stateSignal(preparationKey);
    if (state().generation === generation) {
      state.set({
        ...state(),
        status: 'error',
        errorMessage: error instanceof Error ? error.message : 'تعذر تحضير مراجعة الربط.',
        failureCode: error instanceof LinkingLifecycleError ? error.code : null,
      });
    }
  }

  private stateSignal(preparationKey: string): WritableSignal<LinkingPreparedPreflightState> {
    let state = this.states.get(preparationKey);
    if (state === undefined) {
      state = signal(INITIAL_STATE);
      this.states.set(preparationKey, state);
    }
    return state;
  }

  private requireActor(): string {
    const actorSub = this.authSession.subject();
    if (actorSub === null) {
      throw new Error('يجب تسجيل الدخول قبل تحضير الربط.');
    }
    return actorSub;
  }

  private pollKey(preparationKey: string): string {
    return `preflight:${preparationKey}`;
  }
}

function requirePreparationKey(request: LinkingPreparedPreflightRequest): string {
  if (request.preparationKey === null || request.preparationKey.trim().length === 0) {
    throw new Error('مفتاح تحضير الربط مطلوب.');
  }
  return request.preparationKey;
}
