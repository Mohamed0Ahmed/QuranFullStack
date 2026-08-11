import { Injectable } from '@angular/core';

import { LinkingWorkspaceItem } from '../models/linking-workspace.models';
import { decodeLinkingWorkspace, encodeLinkingWorkspace } from './linking-workspace.codec';
import { LinkingWorkspaceLoadResult, LinkingWorkspaceRepository } from './linking-workspace.repository';

@Injectable({ providedIn: 'root' })
export class LocalStorageLinkingWorkspaceRepository implements LinkingWorkspaceRepository {
  async load(actorSub: string): Promise<LinkingWorkspaceLoadResult> {
    const raw = localStorage.getItem(storageKey(actorSub));
    if (raw === null) {
      return { items: [], invalidPayload: false };
    }
    return decodeLinkingWorkspace(actorSub, raw);
  }

  async save(actorSub: string, revision: number, items: readonly LinkingWorkspaceItem[]): Promise<void> {
    localStorage.setItem(storageKey(actorSub), encodeLinkingWorkspace(actorSub, revision, items));
  }

  async invalidateActiveActor(actorSub: string): Promise<void> {
    localStorage.removeItem(storageKey(actorSub));
  }
}

function storageKey(actorSub: string): string {
  return `qd-linking-workspace:v2:${encodeURIComponent(actorSub)}`;
}
