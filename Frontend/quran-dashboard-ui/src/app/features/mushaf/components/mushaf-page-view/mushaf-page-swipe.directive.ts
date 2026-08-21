import { isPlatformBrowser } from '@angular/common';
import {
  DestroyRef,
  Directive,
  ElementRef,
  NgZone,
  PLATFORM_ID,
  inject,
  output,
} from '@angular/core';

import { QD_BP_MEDIUM_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const SWIPE_DISTANCE_PX = 48;
const HORIZONTAL_DOMINANCE_RATIO = 1.25;
const CLICK_SUPPRESSION_MS = 350;
const EXCLUDED_START_TARGETS =
  'input, textarea, select, a[href], [contenteditable="true"], button:not(.mushaf-word)';

export type MushafPageSwipeDirection = 'next' | 'previous';

@Directive({
  selector: '[qdMushafPageSwipe]',
  standalone: true,
})
export class MushafPageSwipeDirective {
  readonly mushafPageSwipe = output<MushafPageSwipeDirection>();

  private readonly destroyRef = inject(DestroyRef);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly ngZone = inject(NgZone);
  private readonly platformId = inject(PLATFORM_ID);
  private touchLayoutQuery: MediaQueryList | null = null;
  private activePointerId: number | null = null;
  private startX = 0;
  private startY = 0;
  private suppressNextClick = false;
  private clickSuppressionTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    if (!isPlatformBrowser(this.platformId) || typeof window.matchMedia !== 'function') {
      return;
    }

    this.touchLayoutQuery = window.matchMedia(QD_BP_MEDIUM_MAX_QUERY);
    this.ngZone.runOutsideAngular(() => this.attachListeners());
  }

  private readonly onPointerDown = (event: PointerEvent): void => {
    if (event.pointerType !== 'touch') {
      return;
    }

    this.clearClickSuppression();

    if (!event.isPrimary) {
      this.resetGesture();
      return;
    }

    if (!this.touchLayoutQuery?.matches || this.shouldIgnoreStart(event.target)) {
      return;
    }

    this.activePointerId = event.pointerId;
    this.startX = event.clientX;
    this.startY = event.clientY;
  };

  private readonly onPointerUp = (event: PointerEvent): void => {
    if (event.pointerId !== this.activePointerId) {
      return;
    }

    const deltaX = event.clientX - this.startX;
    const deltaY = event.clientY - this.startY;
    this.resetGesture();

    if (!this.isHorizontalSwipe(deltaX, deltaY)) {
      return;
    }

    event.preventDefault();
    this.armClickSuppression();
    const direction: MushafPageSwipeDirection = deltaX > 0 ? 'next' : 'previous';
    this.ngZone.run(() => this.mushafPageSwipe.emit(direction));
  };

  private readonly onPointerCancel = (event: PointerEvent): void => {
    if (event.pointerId === this.activePointerId) {
      this.resetGesture();
    }
  };

  private readonly onCapturedClick = (event: MouseEvent): void => {
    if (!this.suppressNextClick) {
      return;
    }

    this.clearClickSuppression();
    event.preventDefault();
    event.stopImmediatePropagation();
  };

  private attachListeners(): void {
    const element = this.elementRef.nativeElement;
    element.addEventListener('pointerdown', this.onPointerDown);
    element.addEventListener('pointerup', this.onPointerUp);
    element.addEventListener('pointercancel', this.onPointerCancel);
    element.addEventListener('lostpointercapture', this.onPointerCancel);
    element.addEventListener('click', this.onCapturedClick, true);

    this.destroyRef.onDestroy(() => {
      element.removeEventListener('pointerdown', this.onPointerDown);
      element.removeEventListener('pointerup', this.onPointerUp);
      element.removeEventListener('pointercancel', this.onPointerCancel);
      element.removeEventListener('lostpointercapture', this.onPointerCancel);
      element.removeEventListener('click', this.onCapturedClick, true);
      this.clearClickSuppression();
    });
  }

  private shouldIgnoreStart(target: EventTarget | null): boolean {
    return target instanceof Element && target.closest(EXCLUDED_START_TARGETS) !== null;
  }

  private isHorizontalSwipe(deltaX: number, deltaY: number): boolean {
    const horizontalDistance = Math.abs(deltaX);
    const verticalDistance = Math.abs(deltaY);
    return (
      horizontalDistance >= SWIPE_DISTANCE_PX &&
      horizontalDistance >= verticalDistance * HORIZONTAL_DOMINANCE_RATIO
    );
  }

  private armClickSuppression(): void {
    this.clearClickSuppression();
    this.suppressNextClick = true;
    this.clickSuppressionTimer = setTimeout(
      () => this.clearClickSuppression(),
      CLICK_SUPPRESSION_MS,
    );
  }

  private clearClickSuppression(): void {
    this.suppressNextClick = false;
    if (this.clickSuppressionTimer === null) {
      return;
    }

    clearTimeout(this.clickSuppressionTimer);
    this.clickSuppressionTimer = null;
  }

  private resetGesture(): void {
    this.activePointerId = null;
    this.startX = 0;
    this.startY = 0;
  }
}
