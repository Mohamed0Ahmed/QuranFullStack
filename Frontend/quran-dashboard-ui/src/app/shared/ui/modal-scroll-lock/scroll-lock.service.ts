import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';

/**
 * Reference-counted body scroll lock (Feature 029, Change B). Multiple stacked
 * layers (responsive explorer drawer + global detail overlay) may hold the lock
 * simultaneously; the body unlocks only when the last holder releases, and the
 * original overflow value is restored exactly once.
 */
@Injectable({ providedIn: 'root' })
export class ScrollLockService {
  private readonly platformId = inject(PLATFORM_ID);
  private lockCount = 0;
  private previousOverflow = '';

  acquire(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    if (this.lockCount === 0) {
      this.previousOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
    }
    this.lockCount += 1;
  }

  release(): void {
    if (!isPlatformBrowser(this.platformId) || this.lockCount === 0) {
      return;
    }
    this.lockCount -= 1;
    if (this.lockCount === 0) {
      document.body.style.overflow = this.previousOverflow;
    }
  }
}
