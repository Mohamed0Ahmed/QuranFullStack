import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';

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
