import { isPlatformBrowser } from '@angular/common';
import {
  DestroyRef,
  Directive,
  ElementRef,
  PLATFORM_ID,
  afterNextRender,
  inject,
  input,
} from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SessionScrollOffset, SessionScrollStateStore } from './session-scroll-state.store';

export type SessionScrollTarget = 'host' | 'viewport';

const SCROLL_KEYS = new Set([
  'ArrowDown',
  'ArrowLeft',
  'ArrowRight',
  'ArrowUp',
  'End',
  'Home',
  'PageDown',
  'PageUp',
  ' ',
]);

@Directive({
  selector: '[qdSessionScrollState]',
  standalone: true,
})
export class SessionScrollStateDirective {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly store = inject(SessionScrollStateStore);

  readonly qdSessionScrollState = input.required<string>();
  readonly qdSessionScrollTarget = input<SessionScrollTarget>('host');

  private scrollTarget: HTMLElement | Window | null = null;
  private captureFrame: number | null = null;
  private restoreFrame: number | null = null;
  private pendingRestore: SessionScrollOffset | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private mutationObserver: MutationObserver | null = null;
  private activeKey: string | null = null;
  private captureCurrentOnFlush = true;
  private destroyed = false;

  private readonly onScroll = (): void => {
    if (this.pendingRestore !== null) {
      return;
    }
    this.captureCurrentOnFlush = true;
    this.scheduleCapture();
  };
  private readonly onUserScrollIntent = (event: Event): void => {
    if (event.isTrusted) {
      this.cancelPendingRestore();
    }
  };
  private readonly onKeyDown = (event: Event): void => {
    if (event.isTrusted && SCROLL_KEYS.has((event as KeyboardEvent).key)) {
      this.cancelPendingRestore();
    }
  };
  private readonly onPageHide = (): void => this.flush();

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    afterNextRender(() => this.initialize());
    this.destroyRef.onDestroy(() => this.destroy());
  }

  private initialize(): void {
    if (this.destroyed) {
      return;
    }
    const key = this.qdSessionScrollState().trim();
    if (key === '') {
      return;
    }
    this.activeKey = key;
    this.scrollTarget = this.qdSessionScrollTarget() === 'viewport'
      ? window
      : this.elementRef.nativeElement;
    this.router.events
      .pipe(
        filter((event): event is NavigationStart => event instanceof NavigationStart),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.flush());
    window.addEventListener('pagehide', this.onPageHide, { passive: true });
    this.scrollTarget.addEventListener('scroll', this.onScroll, { passive: true });
    this.scrollTarget.addEventListener('wheel', this.onUserScrollIntent, { passive: true });
    this.scrollTarget.addEventListener('touchstart', this.onUserScrollIntent, { passive: true });
    this.scrollTarget.addEventListener('pointerdown', this.onUserScrollIntent, { passive: true });
    this.scrollTarget.addEventListener('keydown', this.onKeyDown, { passive: true });

    const offset = this.store.read(key);
    if (offset === null) {
      if (this.scrollTarget === window) {
        this.pendingRestore = { x: 0, y: 0 };
        this.captureCurrentOnFlush = false;
        this.scheduleRestoreAttempt();
      }
      return;
    }
    this.pendingRestore = offset;
    this.captureCurrentOnFlush = false;
    this.observeContentSize();
    this.scheduleRestoreAttempt();
  }

  private scheduleCapture(): void {
    if (this.captureFrame !== null) {
      return;
    }
    this.captureFrame = window.requestAnimationFrame(() => {
      this.captureFrame = null;
      this.stageCurrentPosition();
    });
  }

  private stageCurrentPosition(): void {
    if (this.scrollTarget === null || this.activeKey === null) {
      return;
    }
    this.store.stage(this.activeKey, this.readPosition());
  }

  private flush(): void {
    if (this.scrollTarget === null) {
      return;
    }
    if (this.captureFrame !== null) {
      window.cancelAnimationFrame(this.captureFrame);
      this.captureFrame = null;
    }
    if (this.captureCurrentOnFlush) {
      this.stageCurrentPosition();
    }
    this.store.flush();
  }

  private observeContentSize(): void {
    this.resizeObserver = typeof ResizeObserver === 'function'
      ? new ResizeObserver(() => this.scheduleRestoreAttempt())
      : null;
    if (this.scrollTarget === window) {
      this.resizeObserver?.observe(document.documentElement);
      if (document.body !== null) {
        this.resizeObserver?.observe(document.body);
      }
      this.observeContentMutations(document.body ?? document.documentElement);
    } else {
      const element = this.scrollTarget as HTMLElement;
      this.resizeObserver?.observe(element);
      if (element.firstElementChild instanceof HTMLElement) {
        this.resizeObserver?.observe(element.firstElementChild);
      }
      this.observeContentMutations(element);
    }
  }

  private observeContentMutations(target: HTMLElement): void {
    if (typeof MutationObserver !== 'function') {
      return;
    }
    this.mutationObserver = new MutationObserver(() => {
      if (this.scrollTarget !== window) {
        const firstChild = (this.scrollTarget as HTMLElement).firstElementChild;
        if (firstChild instanceof HTMLElement) {
          this.resizeObserver?.observe(firstChild);
        }
      }
      this.scheduleRestoreAttempt();
    });
    this.mutationObserver.observe(target, {
      childList: true,
      subtree: true,
      characterData: true,
    });
  }

  private scheduleRestoreAttempt(): void {
    if (this.pendingRestore === null || this.restoreFrame !== null) {
      return;
    }
    this.restoreFrame = window.requestAnimationFrame(() => {
      this.restoreFrame = null;
      this.tryRestore();
    });
  }

  private tryRestore(): void {
    const offset = this.pendingRestore;
    if (offset === null || this.destroyed || !this.isReachable(offset)) {
      return;
    }
    this.pendingRestore = null;
    this.disconnectRestoreObservers();
    this.captureCurrentOnFlush = true;
    this.writePosition(offset);
  }

  private isReachable(offset: SessionScrollOffset): boolean {
    if (this.scrollTarget === window) {
      const root = document.documentElement;
      const body = document.body;
      const maxX = Math.max(root.scrollWidth, body?.scrollWidth ?? 0) - window.innerWidth;
      const maxY = Math.max(root.scrollHeight, body?.scrollHeight ?? 0) - window.innerHeight;
      return Math.abs(offset.x) <= Math.max(0, maxX) + 1 && offset.y <= Math.max(0, maxY) + 1;
    }
    const element = this.scrollTarget as HTMLElement;
    const maxX = element.scrollWidth - element.clientWidth;
    const maxY = element.scrollHeight - element.clientHeight;
    return Math.abs(offset.x) <= Math.max(0, maxX) + 1 && offset.y <= Math.max(0, maxY) + 1;
  }

  private cancelPendingRestore(): void {
    if (this.pendingRestore === null) {
      return;
    }
    this.pendingRestore = null;
    this.captureCurrentOnFlush = false;
    if (this.restoreFrame !== null) {
      window.cancelAnimationFrame(this.restoreFrame);
      this.restoreFrame = null;
    }
    this.disconnectRestoreObservers();
  }

  private disconnectRestoreObservers(): void {
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    this.mutationObserver?.disconnect();
    this.mutationObserver = null;
  }

  private readPosition(): SessionScrollOffset {
    if (this.scrollTarget === window) {
      return { x: window.scrollX, y: window.scrollY };
    }
    const element = this.scrollTarget as HTMLElement;
    return { x: element.scrollLeft, y: element.scrollTop };
  }

  private writePosition(offset: SessionScrollOffset): void {
    if (this.scrollTarget === window) {
      window.scrollTo({ left: offset.x, top: offset.y, behavior: 'auto' });
      return;
    }
    (this.scrollTarget as HTMLElement).scrollTo({
      left: offset.x,
      top: offset.y,
      behavior: 'auto',
    });
  }

  private destroy(): void {
    this.destroyed = true;
    if (this.restoreFrame !== null) {
      window.cancelAnimationFrame(this.restoreFrame);
      this.restoreFrame = null;
    }
    this.flush();
    this.pendingRestore = null;
    this.scrollTarget?.removeEventListener('scroll', this.onScroll);
    this.scrollTarget?.removeEventListener('wheel', this.onUserScrollIntent);
    this.scrollTarget?.removeEventListener('touchstart', this.onUserScrollIntent);
    this.scrollTarget?.removeEventListener('pointerdown', this.onUserScrollIntent);
    this.scrollTarget?.removeEventListener('keydown', this.onKeyDown);
    this.disconnectRestoreObservers();
    window.removeEventListener('pagehide', this.onPageHide);
    this.scrollTarget = null;
    this.activeKey = null;
  }
}
