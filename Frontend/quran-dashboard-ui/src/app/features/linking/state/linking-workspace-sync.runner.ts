import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { HttpLinkingWorkspaceRepository } from '../data-access/http-linking-workspace.repository';
import {
  LinkingWorkspaceRepository,
  LinkingWorkspaceStaleVersionError,
} from '../data-access/linking-workspace.repository';
import { LinkingDataStaleError } from '../models/linking-revision.models';
import {
  LinkingRemovedWorkspaceItem,
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
} from '../models/linking-workspace.models';

const RELOAD_WARNING = 'تعذر حفظ التغيير، وأُعيد تحميل مساحة الربط من الخادم.';
const LOAD_WARNING = 'تعذر تحميل مساحة الربط من الخادم.';

export interface LinkingWorkspaceSyncBindings {
  isCurrentActor(actorSub: string, actorGeneration: number): boolean;
  workspaceVersion(): number | null;
  items(): readonly LinkingWorkspaceItem[];
  findItem(sourceKey: string): LinkingWorkspaceItem | null;
  applySnapshot(snapshot: LinkingWorkspaceSnapshot): void;
  restoreChecked(sourceKey: string): void;
  restoreConfiguration(removed: LinkingRemovedWorkspaceItem): Promise<void>;
  warn(message: string): void;
  invalidateLinkingDataRevision(): void;
}

export type LinkingWorkspaceOperation = (
  workspaceVersion: number | null,
) => Observable<LinkingWorkspaceSnapshot>;

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceSyncRunner {
  private readonly repository: LinkingWorkspaceRepository = inject(HttpLinkingWorkspaceRepository);
  private bindings: LinkingWorkspaceSyncBindings | null = null;
  private queue: Promise<void> = Promise.resolve();

  connect(bindings: LinkingWorkspaceSyncBindings): void {
    this.bindings = bindings;
  }

  async hydrate(actorSub: string, actorGeneration: number): Promise<boolean> {
    const bindings = this.bindings;
    if (bindings === null) {
      return false;
    }
    try {
      const snapshot = await firstValueFrom(this.repository.load());
      if (bindings.isCurrentActor(actorSub, actorGeneration)) {
        bindings.applySnapshot(snapshot);
      }
      return true;
    } catch {
      if (bindings.isCurrentActor(actorSub, actorGeneration)) {
        bindings.warn(LOAD_WARNING);
      }
      return false;
    }
  }

  run(actorSub: string, actorGeneration: number, operation: LinkingWorkspaceOperation): void {
    this.enqueue(actorSub, actorGeneration, async (bindings) => {
      const snapshot = await firstValueFrom(operation(bindings.workspaceVersion()));
      if (bindings.isCurrentActor(actorSub, actorGeneration)) {
        bindings.applySnapshot(snapshot);
      }
    });
  }

  restore(actorSub: string, actorGeneration: number, removed: LinkingRemovedWorkspaceItem): void {
    this.enqueue(actorSub, actorGeneration, async (bindings) => {
      bindings.applySnapshot(
        await firstValueFrom(
          this.repository.addSource(removed.item.source, bindings.workspaceVersion()),
        ),
      );
      await this.restoreConfiguration(bindings, removed);
      await this.restoreOrder(bindings, removed);
      if (removed.wasChecked) {
        bindings.restoreChecked(removed.item.sourceKey);
      }
    });
  }

  private async restoreConfiguration(
    bindings: LinkingWorkspaceSyncBindings,
    removed: LinkingRemovedWorkspaceItem,
  ): Promise<void> {
    await bindings.restoreConfiguration(removed);
  }

  private async restoreOrder(
    bindings: LinkingWorkspaceSyncBindings,
    removed: LinkingRemovedWorkspaceItem,
  ): Promise<void> {
    const sourceIds = orderedSourceIdsWith(bindings.items(), removed);
    if (sourceIds === null) {
      return;
    }
    bindings.applySnapshot(
      await firstValueFrom(this.repository.reorderSources(sourceIds, bindings.workspaceVersion())),
    );
  }

  private enqueue(
    actorSub: string,
    actorGeneration: number,
    work: (bindings: LinkingWorkspaceSyncBindings) => Promise<void>,
  ): void {
    const bindings = this.bindings;
    if (bindings === null) {
      return;
    }
    this.queue = this.queue
      .catch(() => undefined)
      .then(async () => {
        if (!bindings.isCurrentActor(actorSub, actorGeneration)) {
          return;
        }
        try {
          await work(bindings);
        } catch (error: unknown) {
          await this.recover(bindings, actorSub, actorGeneration, error);
        }
      });
  }

  private async recover(
    bindings: LinkingWorkspaceSyncBindings,
    actorSub: string,
    actorGeneration: number,
    error: unknown,
  ): Promise<void> {
    if (!bindings.isCurrentActor(actorSub, actorGeneration)) {
      return;
    }
    const message =
      error instanceof LinkingWorkspaceStaleVersionError || error instanceof Error
        ? error.message
        : RELOAD_WARNING;
    if (error instanceof LinkingDataStaleError) {
      bindings.invalidateLinkingDataRevision();
      bindings.warn(message);
      return;
    }
    try {
      const snapshot = await firstValueFrom(this.repository.load());
      if (bindings.isCurrentActor(actorSub, actorGeneration)) {
        bindings.applySnapshot(snapshot);
        bindings.warn(message);
      }
    } catch {
      if (bindings.isCurrentActor(actorSub, actorGeneration)) {
        bindings.warn(LOAD_WARNING);
      }
    }
  }
}

function orderedSourceIdsWith(
  items: readonly LinkingWorkspaceItem[],
  removed: LinkingRemovedWorkspaceItem,
): readonly number[] | null {
  const restored = items.find((item) => item.sourceKey === removed.item.sourceKey);
  if (restored?.sourceId == null) {
    return null;
  }
  const others = items.filter((item) => item.sourceKey !== removed.item.sourceKey);
  if (others.some((item) => item.sourceId === null)) {
    return null;
  }
  const ordered = [...others];
  ordered.splice(Math.min(removed.index, ordered.length), 0, restored);
  return ordered.map((item) => item.sourceId as number);
}
