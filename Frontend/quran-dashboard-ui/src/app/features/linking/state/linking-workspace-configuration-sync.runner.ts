import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { LinkingWorkspaceDeltaBody } from '../../../core/api/generated/models/linking-workspace-delta-body';
import { LinkingWorkspaceDeltaChangeBody } from '../../../core/api/generated/models/linking-workspace-delta-change-body';
import { LinkingWorkspaceDeltaResponse } from '../../../core/api/generated/models/linking-workspace-delta-response';
import { HttpLinkingWorkspaceConfigurationRepository } from '../data-access/http-linking-workspace-configuration.repository';
import {
  LinkingWorkspaceConfigurationRepository,
  LinkingWorkspaceSourceStaleError,
} from '../data-access/linking-workspace-configuration.repository';
import { LINKING_WORKSPACE_DEBOUNCE_MS } from '../linking.policy';
import { LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import { LinkingDataStaleError } from '../models/linking-revision.models';

export interface LinkingWorkspaceConfigurationSyncBindings {
  reload(sourceId: number): Promise<LinkingOperationSourceDraft | null>;
  acknowledge(sourceKey: string, response: LinkingWorkspaceDeltaResponse): void;
  linkingDataStale(): void;
  conflict(sourceKey: string, message: string): void;
}

interface SourceSyncState {
  acknowledged: LinkingOperationSourceDraft;
  latest: LinkingOperationSourceDraft;
  timer: ReturnType<typeof setTimeout> | null;
  inFlight: boolean;
  queued: boolean;
  rebaseAttempted: boolean;
  flushWaiters: Array<() => void>;
}

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceConfigurationSyncRunner {
  private readonly repository: LinkingWorkspaceConfigurationRepository = inject(
    HttpLinkingWorkspaceConfigurationRepository,
  );
  private readonly states = new Map<string, SourceSyncState>();
  private readonly ready: string[] = [];
  private bindings: LinkingWorkspaceConfigurationSyncBindings | null = null;
  private activeCount = 0;
  private stopped = false;

  connect(bindings: LinkingWorkspaceConfigurationSyncBindings): void {
    this.bindings = bindings;
  }

  track(acknowledged: LinkingOperationSourceDraft): void {
    const existing = this.states.get(acknowledged.sourceKey);
    this.states.set(acknowledged.sourceKey, {
      acknowledged,
      latest: existing?.latest ?? acknowledged,
      timer: existing?.timer ?? null,
      inFlight: existing?.inFlight ?? false,
      queued: existing?.queued ?? false,
      rebaseAttempted: false,
      flushWaiters: existing?.flushWaiters ?? [],
    });
  }

  schedule(draft: LinkingOperationSourceDraft): void {
    if (this.stopped || draft.sourceId === null || draft.sourceVersion === null) {
      return;
    }
    let state = this.states.get(draft.sourceKey);
    if (state === undefined) {
      state = {
        acknowledged: draft,
        latest: draft,
        timer: null,
        inFlight: false,
        queued: false,
        rebaseAttempted: false,
        flushWaiters: [],
      };
      this.states.set(draft.sourceKey, state);
    }
    state.latest = draft;
    if (state.timer !== null) {
      clearTimeout(state.timer);
    }
    state.timer = setTimeout(() => this.enqueue(draft.sourceKey), LINKING_WORKSPACE_DEBOUNCE_MS);
  }

  async flush(sourceKeys: readonly string[]): Promise<void> {
    if (this.stopped) {
      throw new LinkingDataStaleError('تغيّرت بيانات الربط؛ أعد تحميل المصادر قبل المتابعة.');
    }
    const pending = sourceKeys.map((sourceKey) => this.flushSource(sourceKey));
    await Promise.all(pending);
    if (this.stopped) {
      throw new LinkingDataStaleError('تغيّرت بيانات الربط؛ أعد تحميل المصادر قبل المتابعة.');
    }
  }

  remove(sourceKey: string): void {
    const state = this.states.get(sourceKey);
    if (state?.timer !== null && state?.timer !== undefined) {
      clearTimeout(state.timer);
    }
    state?.flushWaiters.forEach((resolve) => resolve());
    this.states.delete(sourceKey);
  }

  resume(): void {
    this.stopped = false;
  }

  async restore(
    acknowledged: LinkingOperationSourceDraft,
    desired: LinkingOperationSourceDraft,
  ): Promise<void> {
    this.track(acknowledged);
    this.schedule(desired);
    await this.flush([desired.sourceKey]);
  }

  private flushSource(sourceKey: string): Promise<void> {
    const state = this.states.get(sourceKey);
    if (state === undefined || sameDraft(state.acknowledged, state.latest)) {
      return Promise.resolve();
    }
    if (state.timer !== null) {
      clearTimeout(state.timer);
      state.timer = null;
    }
    this.enqueue(sourceKey);
    return new Promise((resolve) => state.flushWaiters.push(resolve));
  }

  private enqueue(sourceKey: string): void {
    const state = this.states.get(sourceKey);
    if (state === undefined || state.queued || state.inFlight || this.stopped) {
      return;
    }
    state.timer = null;
    state.queued = true;
    this.ready.push(sourceKey);
    this.drain();
  }

  private drain(): void {
    while (!this.stopped && this.activeCount < 2) {
      const sourceKey = this.ready.shift();
      if (sourceKey === undefined) {
        return;
      }
      const state = this.states.get(sourceKey);
      if (state === undefined || state.inFlight) {
        continue;
      }
      state.queued = false;
      void this.send(sourceKey, state);
    }
  }

  private async send(sourceKey: string, state: SourceSyncState): Promise<void> {
    const changes = toDeltaChanges(state.acknowledged, state.latest);
    if (changes.length === 0) {
      this.finishSource(state);
      return;
    }
    const sourceId = state.acknowledged.sourceId;
    const sourceVersion = state.acknowledged.sourceVersion;
    if (sourceId === null || sourceVersion === null) {
      this.finishSource(state);
      return;
    }
    state.inFlight = true;
    this.activeCount += 1;
    const sent = state.latest;
    try {
      const response = await firstValueFrom(
        this.repository.applyDelta(sourceId, {
          sourceVersion,
          expectedLinkingDataRevision: readRevision(sent),
          changes,
        }),
      );
      state.acknowledged = { ...sent, sourceVersion: response.sourceVersion };
      state.rebaseAttempted = false;
      this.bindings?.acknowledge(sourceKey, response);
    } catch (error: unknown) {
      await this.recover(sourceKey, state, error);
    } finally {
      state.inFlight = false;
      this.activeCount -= 1;
      if (!sameDraft(state.acknowledged, state.latest) && !this.stopped) {
        this.enqueue(sourceKey);
      } else {
        this.finishSource(state);
      }
      this.drain();
    }
  }

  private async recover(sourceKey: string, state: SourceSyncState, error: unknown): Promise<void> {
    if (error instanceof LinkingDataStaleError) {
      this.stopped = true;
      this.ready.length = 0;
      this.bindings?.linkingDataStale();
      this.bindings?.conflict(sourceKey, error.message);
      return;
    }
    if (error instanceof LinkingWorkspaceSourceStaleError && !state.rebaseAttempted) {
      state.rebaseAttempted = true;
      const reloaded = await this.bindings?.reload(state.acknowledged.sourceId!);
      if (reloaded !== null && reloaded !== undefined && readRevision(reloaded) === readRevision(state.latest)) {
        state.acknowledged = reloaded;
        return;
      }
    }
    this.bindings?.conflict(
      sourceKey,
      error instanceof Error ? error.message : 'تعذر مزامنة إعدادات المصدر.',
    );
    state.latest = state.acknowledged;
  }

  private finishSource(state: SourceSyncState): void {
    const waiters = state.flushWaiters.splice(0);
    waiters.forEach((resolve) => resolve());
  }
}

function readRevision(draft: LinkingOperationSourceDraft): number {
  if (draft.linkingDataRevision <= 0) {
    throw new Error('مراجعة بيانات الربط مطلوبة لمزامنة المصدر.');
  }
  return draft.linkingDataRevision;
}

function sameDraft(left: LinkingOperationSourceDraft, right: LinkingOperationSourceDraft): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function toDeltaChanges(
  acknowledged: LinkingOperationSourceDraft,
  latest: LinkingOperationSourceDraft,
): LinkingWorkspaceDeltaChangeBody[] {
  const changes: LinkingWorkspaceDeltaChangeBody[] = [];
  if (acknowledged.label !== latest.label) {
    changes.push(change('set-label', { label: latest.label }));
  }
  if (JSON.stringify(acknowledged.selection) !== JSON.stringify(latest.selection)) {
    changes.push(
      change('replace-inclusion', {
        mode: latest.selection.mode === 'all-except' ? 'all_except' : 'only',
        ayahOverrideIds: [...latest.selection.ayahIds],
      }),
    );
  }
  if (acknowledged.automaticWordMatchesEnabled !== latest.automaticWordMatchesEnabled) {
    changes.push(
      change('set-automatic-word-matches', { enabled: latest.automaticWordMatchesEnabled }),
    );
  }
  if (acknowledged.manualLinkShape !== latest.manualLinkShape && latest.manualLinkShape !== null) {
    changes.push(change('set-manual-link-shape', { shape: latest.manualLinkShape }));
  }
  appendWordChanges(changes, acknowledged, latest);
  appendDescriptionChanges(changes, acknowledged, latest);
  return changes;
}

function appendWordChanges(
  changes: LinkingWorkspaceDeltaChangeBody[],
  acknowledged: LinkingOperationSourceDraft,
  latest: LinkingOperationSourceDraft,
): void {
  const ayahIds = new Set([
    ...Object.keys(acknowledged.selectedWordIdsByAyahId).map(Number),
    ...Object.keys(latest.selectedWordIdsByAyahId).map(Number),
  ]);
  for (const ayahId of ayahIds) {
    const before = new Set(acknowledged.selectedWordIdsByAyahId[ayahId] ?? []);
    const after = new Set(latest.selectedWordIdsByAyahId[ayahId] ?? []);
    for (const wordId of new Set([...before, ...after])) {
      if (before.has(wordId) !== after.has(wordId)) {
        changes.push(change('set-word-selected', { ayahId, quranWordId: wordId, selected: after.has(wordId) }));
      }
    }
  }
}

function appendDescriptionChanges(
  changes: LinkingWorkspaceDeltaChangeBody[],
  acknowledged: LinkingOperationSourceDraft,
  latest: LinkingOperationSourceDraft,
): void {
  const byAyah = (draft: LinkingOperationSourceDraft) => {
    const grouped = new Map<number, typeof draft.descriptions>();
    for (const description of draft.descriptions) {
      grouped.set(description.ayahId, [...(grouped.get(description.ayahId) ?? []), description]);
    }
    return grouped;
  };
  const before = byAyah(acknowledged);
  const after = byAyah(latest);
  for (const ayahId of new Set([...before.keys(), ...after.keys()])) {
    const oldBodies = (before.get(ayahId) ?? []).map((value) => value.body);
    const newBodies = (after.get(ayahId) ?? []).map((value) => value.body);
    if (JSON.stringify(oldBodies) !== JSON.stringify(newBodies)) {
      changes.push(change('replace-ayah-descriptions', { ayahId, descriptions: newBodies }));
    }
  }
}

function change(
  kind: string,
  values: Partial<LinkingWorkspaceDeltaChangeBody>,
): LinkingWorkspaceDeltaChangeBody {
  return {
    kind,
    label: null,
    ayahId: null,
    included: null,
    mode: null,
    ayahOverrideIds: null,
    quranWordId: null,
    selected: null,
    enabled: null,
    shape: null,
    descriptions: null,
    ...values,
  };
}
