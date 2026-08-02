import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivationEnd,
  ActivationStart,
  ChildActivationEnd,
  ChildActivationStart,
  GuardsCheckEnd,
  GuardsCheckStart,
  NavigationStart,
  ResolveEnd,
  ResolveStart,
  RouteConfigLoadEnd,
  RouteConfigLoadStart,
  Router,
  RoutesRecognized,
} from '@angular/router';

// Warm (chunk-cached) navigations here settle in well under 100ms; 200ms keeps every one of
// them flash-free while still appearing early inside the 500–1000ms cold-chunk gap.
export const NAV_PROGRESS_SHOW_DELAY_MS = 200;
// Covers the done state's snap-to-full plus the --qd-t-base fade before removal.
const DONE_LINGER_MS = 400;

// The known lifecycle events emitted *between* NavigationStart and a navigation's terminal
// event. The whitelist is deliberately of in-flight events, not terminal ones: anything not
// listed here (and not a NavigationStart) settles the bar — End/Cancel/Error/Skipped today,
// and any event class a future Angular adds. An unknown event therefore fails closed (the bar
// clears early) instead of sticking forever behind an incomplete terminal whitelist.
const IN_FLIGHT_EVENT_CLASSES = [
  RouteConfigLoadStart,
  RouteConfigLoadEnd,
  RoutesRecognized,
  GuardsCheckStart,
  GuardsCheckEnd,
  ChildActivationStart,
  ChildActivationEnd,
  ActivationStart,
  ActivationEnd,
  ResolveStart,
  ResolveEnd,
] as const;

type NavProgressState = 'idle' | 'pending' | 'visible' | 'done';

@Component({
  selector: 'qd-nav-progress',
  standalone: true,
  templateUrl: './nav-progress.component.html',
  styleUrl: './nav-progress.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavProgressComponent {
  private showTimer: ReturnType<typeof setTimeout> | undefined;
  private doneTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly state = signal<NavProgressState>('idle');
  protected readonly barVisible = computed(
    () => this.state() === 'visible' || this.state() === 'done',
  );
  // Populated only once the bar actually shows, so warm navigations announce nothing and the
  // shell never queues chatter against a page-owned polite region (e.g. abwab's announcer).
  // The text stays through 'done' so a still-queued announcement is not cancelled mid-fade.
  protected readonly statusMessage = computed(() =>
    this.barVisible() ? 'جارٍ تحميل الصفحة…' : '',
  );

  constructor() {
    inject(Router)
      .events.pipe(takeUntilDestroyed())
      .subscribe((event) => {
        if (event instanceof NavigationStart) {
          this.arm();
        } else if (!IN_FLIGHT_EVENT_CLASSES.some((eventClass) => event instanceof eventClass)) {
          this.settle();
        }
      });
    inject(DestroyRef).onDestroy(() => this.clearTimers());
  }

  private arm(): void {
    this.clearTimers();
    this.state.set('pending');
    this.showTimer = setTimeout(() => this.state.set('visible'), NAV_PROGRESS_SHOW_DELAY_MS);
  }

  private settle(): void {
    if (this.state() === 'idle') {
      return;
    }
    this.clearTimers();
    if (this.state() === 'pending') {
      this.state.set('idle');
      return;
    }
    this.state.set('done');
    this.doneTimer = setTimeout(() => this.state.set('idle'), DONE_LINGER_MS);
  }

  private clearTimers(): void {
    if (this.showTimer !== undefined) {
      clearTimeout(this.showTimer);
      this.showTimer = undefined;
    }
    if (this.doneTimer !== undefined) {
      clearTimeout(this.doneTimer);
      this.doneTimer = undefined;
    }
  }
}
