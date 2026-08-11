import { Injectable } from '@angular/core';

import {
  LinkingSelection,
  LinkingWorkspaceItem,
  LinkingWorkspaceSessionEnvelope,
  LinkingWorkspaceSessionItem,
} from '../models/linking-workspace.models';
import { isLinkingSourceDescriptor, isVerseKey } from '../models/linking-source.models';
import { linkingSourceKey } from '../utils/linking-source-key';

const SESSION_STORAGE_KEY = 'qd-linking-workspace-v1';
const SESSION_VERSION = 1;

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceSession {
  load(actorSub: string): readonly LinkingWorkspaceItem[] | null {
    const envelope = this.readEnvelope();
    if (!envelope) {
      return null;
    }

    if (envelope.actorSub !== actorSub) {
      this.clear();
      return null;
    }

    return readWorkspaceItems(envelope.items);
  }

  save(actorSub: string, items: readonly LinkingWorkspaceItem[]): void {
    const envelope: LinkingWorkspaceSessionEnvelope = {
      version: SESSION_VERSION,
      actorSub,
      items: items.map(toSessionItem),
    };

    try {
      sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(envelope));
    } catch {
      return;
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(SESSION_STORAGE_KEY);
    } catch {
      return;
    }
  }

  private readEnvelope(): LinkingWorkspaceSessionEnvelope | null {
    let raw: string | null;

    try {
      raw = sessionStorage.getItem(SESSION_STORAGE_KEY);
    } catch {
      return null;
    }

    if (!raw) {
      return null;
    }

    return parseEnvelope(raw);
  }
}

function parseEnvelope(raw: string): LinkingWorkspaceSessionEnvelope | null {
  let parsed: unknown;

  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  if (!isRecord(parsed) || parsed['version'] !== SESSION_VERSION || !isNonBlankString(parsed['actorSub'])) {
    return null;
  }

  const items = parsed['items'];
  if (!Array.isArray(items)) {
    return null;
  }

  return {
    version: SESSION_VERSION,
    actorSub: parsed['actorSub'],
    items: items.filter(isWorkspaceSessionItem),
  };
}

function readWorkspaceItems(items: readonly LinkingWorkspaceSessionItem[]): readonly LinkingWorkspaceItem[] {
  const sourceKeys = new Set<string>();
  const workspaceItems: LinkingWorkspaceItem[] = [];

  for (const item of items) {
    const sourceKey = linkingSourceKey(item.source);
    if (item.sourceKey !== sourceKey || sourceKeys.has(sourceKey)) {
      continue;
    }

    sourceKeys.add(sourceKey);
    workspaceItems.push({
      sourceKey,
      source: item.source,
      selection: normalizeSelection(item.selection),
      resultCount: item.resultCount,
      highlightSourceWords: item.highlightSourceWords,
    });
  }

  return workspaceItems;
}

function toSessionItem(item: LinkingWorkspaceItem): LinkingWorkspaceSessionItem {
  return {
    sourceKey: item.sourceKey,
    source: item.source,
    selection: item.selection,
    resultCount: item.resultCount,
    highlightSourceWords: item.highlightSourceWords,
  };
}

function isWorkspaceSessionItem(value: unknown): value is LinkingWorkspaceSessionItem {
  if (!isRecord(value) || !isLinkingSourceDescriptor(value['source'])) {
    return false;
  }

  return (
    typeof value['sourceKey'] === 'string' &&
    isLinkingSelection(value['selection']) &&
    isResultCount(value['resultCount']) &&
    typeof value['highlightSourceWords'] === 'boolean'
  );
}

function isLinkingSelection(value: unknown): value is LinkingSelection {
  if (!isRecord(value) || (value['mode'] !== 'all-except' && value['mode'] !== 'only')) {
    return false;
  }

  return Array.isArray(value['verseKeys']) && value['verseKeys'].every(isVerseKey);
}

function normalizeSelection(selection: LinkingSelection): LinkingSelection {
  return {
    mode: selection.mode,
    verseKeys: [...new Set(selection.verseKeys)],
  };
}

function isResultCount(value: unknown): value is number | null {
  return value === null || (typeof value === 'number' && Number.isSafeInteger(value) && value >= 0);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isNonBlankString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}
