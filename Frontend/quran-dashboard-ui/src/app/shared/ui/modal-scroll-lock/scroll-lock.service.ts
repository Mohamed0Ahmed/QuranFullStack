import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';

// Reference-counted so stacked layers (drawer + detail overlay) can hold the lock at
// once: the body unlocks only when the last holder releases, restoring the original
// overflow value exactly once.
//
// `isLocked` (Slice B2, T904) is the one piece of state the chrome-inert rule reads —
// `.qd-navbar` inerts itself off it rather than a second "any modal open" service, since
// every modal dialog in the app already acquires this same lock (UI_STYLE_SYSTEM.md §17
// "Chrome-inert rule").
@Injectable({ providedIn: 'root' })
export class ScrollLockService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly lockCount = signal(0);
  private previousOverflow = '';

  readonly isLocked = computed(() => this.lockCount() > 0);

  acquire(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    if (this.lockCount() === 0) {
      this.previousOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
    }
    this.lockCount.update((count) => count + 1);
  }

  release(): void {
    if (!isPlatformBrowser(this.platformId) || this.lockCount() === 0) {
      return;
    }
    this.lockCount.update((count) => count - 1);
    if (this.lockCount() === 0) {
      document.body.style.overflow = this.previousOverflow;
    }
  }
}
