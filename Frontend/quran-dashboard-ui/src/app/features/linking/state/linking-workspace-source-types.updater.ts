import { Injectable, WritableSignal, inject, signal } from '@angular/core';

import { HttpLinkingWorkspaceRepository } from '../data-access/http-linking-workspace.repository';
import { LinkingWorkspaceRepository } from '../data-access/linking-workspace.repository';
import { LinkingWorkspaceItem } from '../models/linking-workspace.models';
import { linkingSourceKey } from '../utils/linking-source-key';
import {
  linkingSourceTypeCodes,
  linkingSourceSupportsTypeFilters,
  withLinkingSourceTypeCodes,
} from '../utils/linking-source-types';
import { LinkingWorkspaceConfigurationSyncRunner } from './linking-workspace-configuration-sync.runner';
import { LinkingWorkspaceSyncRunner } from './linking-workspace-sync.runner';

export interface LinkingWorkspaceSourceTypeBindings {
  canMutate(): boolean;
  findItem(sourceKey: string): LinkingWorkspaceItem | null;
  actor(): { readonly sub: string; readonly generation: number } | null;
  isChecked(sourceKey: string): boolean;
  items: WritableSignal<readonly LinkingWorkspaceItem[]>;
  checkedSourceKeys: WritableSignal<readonly string[]>;
  editorSourceKey: WritableSignal<string | null>;
  cancelPage(scope: string): void;
  warn(message: string): void;
}

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceSourceTypesUpdater {
  private readonly sync = inject(LinkingWorkspaceSyncRunner);
  private readonly configurationSync = inject(LinkingWorkspaceConfigurationSyncRunner);
  private readonly repository: LinkingWorkspaceRepository = inject(HttpLinkingWorkspaceRepository);
  private readonly pendingSourceIds = signal<readonly number[]>([]);
  private bindings: LinkingWorkspaceSourceTypeBindings | null = null;

  connect(bindings: LinkingWorkspaceSourceTypeBindings): void {
    this.bindings = bindings;
  }

  set(sourceKey: string, typeCodes: readonly string[]): void {
    const bindings = this.requireBindings();
    const item = bindings.findItem(sourceKey);
    if (
      !bindings.canMutate()
      || item === null
      || item.sourceId === null
      || item.sourceVersion === null
      || !linkingSourceSupportsTypeFilters(item.source)
      || this.isPending(item.sourceId)
    ) {
      return;
    }
    const descriptor = withLinkingSourceTypeCodes(item.source, typeCodes);
    if (linkingSourceKey(descriptor) === linkingSourceKey(item.source)) {
      return;
    }
    const sourceId = item.sourceId;
    this.pendingSourceIds.update((ids) => [...ids, sourceId]);
    void this.persist(item.sourceKey, linkingSourceTypeCodes(descriptor));
  }

  isPending(sourceId: number | null): boolean {
    return sourceId !== null && this.pendingSourceIds().includes(sourceId);
  }

  reset(): void {
    this.pendingSourceIds.set([]);
  }

  complete(sourceId: number): void {
    this.pendingSourceIds.update((ids) => ids.filter((id) => id !== sourceId));
  }

  remap(sourceId: number, previousSourceKey: string, wasChecked: boolean): void {
    const bindings = this.requireBindings();
    const replacement = bindings.items().find((item) => item.sourceId === sourceId);
    if (replacement === undefined) {
      return;
    }
    if (wasChecked) {
      bindings.checkedSourceKeys.update((keys) =>
        keys.includes(replacement.sourceKey) ? keys : [...keys, replacement.sourceKey],
      );
    }
    if (bindings.editorSourceKey() === previousSourceKey) {
      bindings.editorSourceKey.set(replacement.sourceKey);
    }
    bindings.cancelPage(`workspace-editor:${previousSourceKey}`);
  }

  private async persist(sourceKey: string, typeCodes: readonly string[]): Promise<void> {
    const bindings = this.requireBindings();
    const initial = bindings.findItem(sourceKey);
    if (initial?.sourceId == null) {
      return;
    }
    try {
      await this.configurationSync.flush([sourceKey]);
      const current = bindings.findItem(sourceKey);
      const actor = bindings.actor();
      if (
        current?.sourceId !== initial.sourceId
        || current.sourceVersion === null
        || actor === null
      ) {
        this.complete(initial.sourceId);
        return;
      }
      const wasChecked = bindings.isChecked(sourceKey);
      const sourceId = current.sourceId;
      const sourceVersion = current.sourceVersion;
      this.configurationSync.remove(sourceKey);
      this.sync.replaceSourceTypes(
        actor.sub,
        actor.generation,
        sourceId,
        sourceKey,
        wasChecked,
        (workspaceVersion) => this.repository.updateSourceTypes(
          sourceId,
          typeCodes,
          sourceVersion,
          workspaceVersion,
        ),
      );
    } catch (error: unknown) {
      this.complete(initial.sourceId);
      bindings.warn(error instanceof Error ? error.message : 'تعذر حفظ أنواع كلمات المصدر.');
    }
  }

  private requireBindings(): LinkingWorkspaceSourceTypeBindings {
    if (this.bindings === null) {
      throw new Error('Linking workspace source type updater is not connected.');
    }
    return this.bindings;
  }
}
