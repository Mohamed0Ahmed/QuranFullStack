import { isPlatformBrowser } from '@angular/common';
import { DestroyRef, Injectable, PLATFORM_ID, inject } from '@angular/core';

const STORAGE_KEY = 'qd.words.search-drafts.v1';
const STORAGE_VERSION = 1;
const MAX_DRAFTS = 5;
const MAX_STORED_VALUE_LENGTH = 2048;

export const WORDS_SEARCH_DRAFT_KEYS = {
  unique: 'unique',
  roots: 'roots',
  lemmas: 'lemmas',
  stems: 'stems',
  types: 'types',
} as const;

export type WordsSearchDraftKey = typeof WORDS_SEARCH_DRAFT_KEYS[keyof typeof WORDS_SEARCH_DRAFT_KEYS];

interface StoredWordsSearchDrafts {
  readonly v: number;
  readonly d: Partial<Record<WordsSearchDraftKey, readonly [string, string]>>;
}

interface WordsSearchDraftEntry {
  readonly base: string;
  readonly draft: string;
}

@Injectable({ providedIn: 'root' })
export class WordsSearchDraftSessionStore {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);
  private readonly drafts = new Map<WordsSearchDraftKey, WordsSearchDraftEntry>();
  private dirty = false;
  private readonly onPageHide = (): void => this.flush();

  constructor() {
    this.load();
    if (isPlatformBrowser(this.platformId)) {
      window.addEventListener('pagehide', this.onPageHide, { passive: true });
      this.destroyRef.onDestroy(() => {
        this.flush();
        window.removeEventListener('pagehide', this.onPageHide);
      });
    }
  }

  resolve(key: WordsSearchDraftKey, committedValue: string, allowRebase: boolean): string {
    const entry = this.drafts.get(key);
    if (entry === undefined) {
      return committedValue;
    }
    if (entry.draft === committedValue) {
      this.clear(key);
      return committedValue;
    }
    if (entry.base === committedValue) {
      return entry.draft;
    }
    if (allowRebase) {
      this.drafts.set(key, { base: committedValue, draft: entry.draft });
      this.dirty = true;
      return entry.draft;
    }
    this.clear(key);
    return committedValue;
  }

  stage(key: WordsSearchDraftKey, draft: string, committedValue: string): void {
    if (
      draft === committedValue ||
      draft.length > MAX_STORED_VALUE_LENGTH ||
      committedValue.length > MAX_STORED_VALUE_LENGTH
    ) {
      if (this.drafts.delete(key)) {
        this.dirty = true;
      }
      return;
    }
    const current = this.drafts.get(key);
    if (current?.draft === draft && current.base === committedValue) {
      return;
    }
    this.drafts.set(key, { base: committedValue, draft });
    this.dirty = true;
  }

  flush(): void {
    if (!this.dirty || !isPlatformBrowser(this.platformId)) {
      return;
    }
    const state: StoredWordsSearchDrafts = {
      v: STORAGE_VERSION,
      d: Object.fromEntries(
        [...this.drafts].map(([key, entry]) => [key, [entry.base, entry.draft] as const]),
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
      const parsed = JSON.parse(raw) as Partial<StoredWordsSearchDrafts>;
      if (parsed.v !== STORAGE_VERSION || parsed.d === null || typeof parsed.d !== 'object') {
        return;
      }
      for (const [key, value] of Object.entries(parsed.d).slice(-MAX_DRAFTS)) {
        if (
          isWordsSearchDraftKey(key) &&
          Array.isArray(value) &&
          value.length === 2 &&
          typeof value[0] === 'string' &&
          typeof value[1] === 'string' &&
          value[0].length <= MAX_STORED_VALUE_LENGTH &&
          value[1].length <= MAX_STORED_VALUE_LENGTH
        ) {
          this.drafts.set(key, { base: value[0], draft: value[1] });
        }
      }
    } catch {
      this.drafts.clear();
    }
  }

  private clear(key: WordsSearchDraftKey): void {
    if (this.drafts.delete(key)) {
      this.dirty = true;
      this.flush();
    }
  }
}

function isWordsSearchDraftKey(value: string): value is WordsSearchDraftKey {
  return Object.values(WORDS_SEARCH_DRAFT_KEYS).some((key) => key === value);
}
