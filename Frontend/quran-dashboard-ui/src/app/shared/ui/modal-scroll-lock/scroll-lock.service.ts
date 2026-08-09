import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';

export interface ScrollLockHandle {
  release(): void;
}

@Injectable({ providedIn: 'root' })
export class ScrollLockService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly holders = signal<readonly symbol[]>([]);
  private readonly anonymousHolders: symbol[] = [];
  private previousOverflow = '';

  readonly isLocked = computed(() => this.holders().length > 0);
  readonly holderCount = computed(() => this.holders().length);

  hold(): ScrollLockHandle {
    const token = Symbol('qd-scroll-lock');
    this.add(token);
    let released = false;
    return {
      release: () => {
        if (released) {
          return;
        }
        released = true;
        this.remove(token);
      },
    };
  }

  acquire(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const token = Symbol('qd-scroll-lock-anonymous');
    this.anonymousHolders.push(token);
    this.add(token);
  }

  release(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const token = this.anonymousHolders.pop();
    if (token === undefined) {
      return;
    }
    this.remove(token);
  }

  private add(token: symbol): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    if (this.holders().length === 0) {
      this.previousOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
    }
    this.holders.update((current) => [...current, token]);
  }

  private remove(token: symbol): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const current = this.holders();
    const index = current.indexOf(token);
    if (index < 0) {
      return;
    }
    const next = current.slice(0, index).concat(current.slice(index + 1));
    this.holders.set(next);
    if (next.length === 0) {
      document.body.style.overflow = this.previousOverflow;
    }
  }
}
