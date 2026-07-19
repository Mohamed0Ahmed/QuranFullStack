import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { WordsExplainerKey } from '../models/words-explainer.content';

// Value is the comma-joined set of COLLAPSED keys; an absent/empty entry means every hero is expanded.
const STORAGE_KEY = 'qd-words-explainer';

// The collapsed set is read from localStorage in a field initialiser (not an effect/ngOnInit) so a
// hero's stored state is known BEFORE first paint; an async read would render expanded then collapse —
// the Feature 030 N3 layout jolt (plan §4.3). Keys are per page (plan §3.3); default is expanded (D2).
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
