import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  DestroyRef,
  Directive,
  ElementRef,
  NgZone,
  PLATFORM_ID,
  Renderer2,
  inject,
  input,
  output,
} from '@angular/core';

import type { DashboardSectionKey } from '../models/dashboard-home.models';

@Directive({
  selector: '[qdDashboardSectionObserver]',
  standalone: true,
})
export class DashboardSectionObserverDirective implements AfterViewInit {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly zone = inject(NgZone);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);

  readonly sectionKey = input.required<DashboardSectionKey>({
    alias: 'qdDashboardSectionObserver',
  });
  readonly sectionActive = output<DashboardSectionKey>();

  ngAfterViewInit(): void {
    const host = this.element.nativeElement;
    if (!isPlatformBrowser(this.platformId) || typeof IntersectionObserver === 'undefined') {
      this.renderer.addClass(host, 'dashboard-section-motion--visible');
      return;
    }

    const revealObserver = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (!entry?.isIntersecting) {
          return;
        }

        this.renderer.addClass(host, 'dashboard-section-motion--visible');
      },
      {
        rootMargin: '0px 0px -20% 0px',
        threshold: 0.08,
      },
    );

    const activeObserver = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          this.zone.run(() => this.sectionActive.emit(this.sectionKey()));
        }
      },
      {
        rootMargin: '-25% 0px -74% 0px',
        threshold: 0,
      },
    );

    revealObserver.observe(host);
    activeObserver.observe(host);
    this.destroyRef.onDestroy(() => {
      revealObserver.disconnect();
      activeObserver.disconnect();
    });
  }
}
