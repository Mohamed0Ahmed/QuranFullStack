import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { WordsExplainerKey } from '../models/words-explainer.content';

const STORAGE_KEY = 'qd-words-explainer';

// Read in a field initialiser (not an effect) so stored state is known BEFORE first paint;
// an async read would render expanded then collapse (layout jolt).
@Injectable({ providedIn: 'root' })
export class WordsExplainerPreference {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly collapsed = new Set<string>(this.readCollapsed());

  isExpanded(key: WordsExplainerKey): boolean {
    return !this.collapsed.has(key);
  }

  setExpanded(key: WordsExplainerKey, expanded: boolean): void {
    if (expanded) {
      this.collapsed.delete(key);
    } else {
      this.collapsed.add(key);
    }
    this.writeCollapsed();
  }

  private readCollapsed(): string[] {
    if (!this.isBrowser) {
      return [];
    }
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return [];
      }
      return raw
        .split(',')
        .map((key) => key.trim())
        .filter((key) => key.length > 0);
    } catch {
      return [];
    }
  }

  private writeCollapsed(): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      localStorage.setItem(STORAGE_KEY, [...this.collapsed].join(','));
    } catch {
      // Storage unavailable (private mode / quota): the in-memory set still drives this session.
    }
  }
}
