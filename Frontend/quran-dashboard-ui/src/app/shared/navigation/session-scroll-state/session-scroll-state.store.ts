import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';

const STORAGE_KEY = 'qd.scroll.resume.v1';
const STORAGE_VERSION = 1;
const MAX_ENTRIES = 24;
const MAX_KEY_LENGTH = 160;
const MAX_OFFSET = 10_000_000;

export interface SessionScrollOffset {
  readonly x: number;
  readonly y: number;
}

interface StoredScrollState {
  readonly v: number;
  readonly o: Record<string, readonly [number, number]>;
}

@Injectable({ providedIn: 'root' })
export class SessionScrollStateStore {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly offsets = new Map<string, SessionScrollOffset>();
  private dirty = false;

  constructor() {
    this.load();
  }

  read(key: string): SessionScrollOffset | null {
    return this.offsets.get(normalizeKey(key)) ?? null;
  }

  stage(key: string, offset: SessionScrollOffset): void {
    const normalizedKey = normalizeKey(key);
    const normalizedOffset = normalizeOffset(offset);
    if (normalizedKey === '' || normalizedOffset === null) {
      return;
    }

    this.offsets.delete(normalizedKey);
    this.offsets.set(normalizedKey, normalizedOffset);
    while (this.offsets.size > MAX_ENTRIES) {
      const oldestKey = this.offsets.keys().next().value as string | undefined;
      if (oldestKey === undefined) {
        break;
      }
      this.offsets.delete(oldestKey);
    }
    this.dirty = true;
  }

  flush(): void {
    if (!this.dirty || !isPlatformBrowser(this.platformId)) {
      return;
    }
    const state: StoredScrollState = {
      v: STORAGE_VERSION,
      o: Object.fromEntries(
        [...this.offsets].map(([key, offset]) => [key, [offset.x, offset.y] as const]),
      ),
    };
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
      this.dirty = false;
    } catch {
      return;
    }
  }

  private load(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw === null) {
        return;
      }
      const parsed = JSON.parse(raw) as Partial<StoredScrollState>;
      if (parsed.v !== STORAGE_VERSION || parsed.o === null || typeof parsed.o !== 'object') {
        return;
      }
      for (const [key, pair] of Object.entries(parsed.o).slice(-MAX_ENTRIES)) {
        const normalizedKey = normalizeKey(key);
        const offset = Array.isArray(pair) && pair.length === 2
          ? normalizeOffset({ x: pair[0], y: pair[1] })
          : null;
        if (normalizedKey !== '' && offset !== null) {
          this.offsets.set(normalizedKey, offset);
        }
      }
    } catch {
      this.offsets.clear();
    }
  }
}

function normalizeKey(key: string): string {
  const normalized = key.trim();
  return normalized.length > 0 && normalized.length <= MAX_KEY_LENGTH ? normalized : '';
}

function normalizeOffset(offset: SessionScrollOffset): SessionScrollOffset | null {
  if (!Number.isFinite(offset.x) || !Number.isFinite(offset.y)) {
    return null;
  }
  return {
    x: Math.max(-MAX_OFFSET, Math.min(MAX_OFFSET, Math.round(offset.x))),
    y: Math.max(0, Math.min(MAX_OFFSET, Math.round(offset.y))),
  };
}
