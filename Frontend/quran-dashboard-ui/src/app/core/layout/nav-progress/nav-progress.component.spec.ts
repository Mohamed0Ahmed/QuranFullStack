import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import {
  GuardsCheckStart,
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationSkipped,
  NavigationStart,
  ResolveStart,
  RouteConfigLoadStart,
  Router,
  RoutesRecognized,
} from '@angular/router';
import { Subject } from 'rxjs';

import { NavProgressComponent } from './nav-progress.component';

// Not a real Angular router event: proves the settle rule is an inversion (anything that is
// neither NavigationStart nor a known in-flight event clears the bar), so a future router
// event class cannot leave the bar stuck.
class FutureUnknownRouterEvent {}

describe('NavProgressComponent', () => {
  let events: Subject<unknown>;

  beforeEach(() => {
    vi.useFakeTimers();
    getTestBed().resetTestingModule();
    events = new Subject<unknown>();
    TestBed.configureTestingModule({
      imports: [NavProgressComponent],
      providers: [{ provide: Router, useValue: { events } }],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
    vi.useRealTimers();
  });

  function render() {
    const fixture = TestBed.createComponent(NavProgressComponent);
    fixture.detectChanges();
    return fixture;
  }

  function bar(fixture: ReturnType<typeof render>): HTMLElement | null {
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-nav-progress"]');
  }

  function statusText(fixture: ReturnType<typeof render>): string {
    fixture.detectChanges();
    return (
      (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="qd-nav-progress-status"]')
        ?.textContent?.trim() ?? ''
    );
  }

  it('shows nothing until the 200ms show-delay elapses, then shows the bar', () => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    expect(bar(fixture)).toBeNull();

    vi.advanceTimersByTime(199);
    expect(bar(fixture)).toBeNull();

    vi.advanceTimersByTime(1);
    expect(bar(fixture)).not.toBeNull();
  });

  it('never shows for a navigation that settles before the delay (warm navigation)', () => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(50);
    events.next(new NavigationEnd(1, '/abwab', '/abwab'));
    vi.advanceTimersByTime(1000);

    expect(bar(fixture)).toBeNull();
  });

  it('in-flight lifecycle events keep the pending bar alive instead of settling it', () => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    events.next(new RouteConfigLoadStart({}));
    vi.advanceTimersByTime(100);
    events.next(new RoutesRecognized(1, '/abwab', '/abwab', null as never));
    events.next(new GuardsCheckStart(1, '/abwab', '/abwab', null as never));
    events.next(new ResolveStart(1, '/abwab', '/abwab', null as never));
    vi.advanceTimersByTime(100);

    expect(bar(fixture)).not.toBeNull();
  });

  const terminalEvents: ReadonlyArray<[string, () => unknown]> = [
    ['NavigationEnd', () => new NavigationEnd(1, '/abwab', '/abwab')],
    ['NavigationCancel', () => new NavigationCancel(1, '/abwab', '')],
    ['NavigationError', () => new NavigationError(1, '/abwab', new Error('chunk failed'))],
    ['NavigationSkipped', () => new NavigationSkipped(1, '/abwab', '')],
    ['an unknown future event class', () => new FutureUnknownRouterEvent()],
  ];

  it.each(terminalEvents)('clears a visible bar on %s', (_name, makeEvent) => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(200);
    expect(bar(fixture)).not.toBeNull();

    events.next(makeEvent());
    vi.advanceTimersByTime(400);
    expect(bar(fixture)).toBeNull();
  });

  it.each(terminalEvents)('cancels a still-pending bar on %s', (_name, makeEvent) => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(100);
    events.next(makeEvent());
    vi.advanceTimersByTime(1000);

    expect(bar(fixture)).toBeNull();
  });

  it('settling a visible bar passes through the done fade state before removal', () => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(200);
    events.next(new NavigationEnd(1, '/abwab', '/abwab'));

    const doneBar = bar(fixture);
    expect(doneBar?.classList.contains('qd-nav-progress--done')).toBe(true);

    vi.advanceTimersByTime(400);
    expect(bar(fixture)).toBeNull();
  });

  it('a new navigation while the bar is fading re-arms the show-delay cleanly', () => {
    const fixture = render();

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(200);
    events.next(new NavigationEnd(1, '/abwab', '/abwab'));
    events.next(new NavigationStart(2, '/dashboard/words'));

    expect(bar(fixture)).toBeNull();
    vi.advanceTimersByTime(200);
    expect(bar(fixture)).not.toBeNull();
  });

  it('announces politely only when the bar actually shows, and stays silent on warm navigations', () => {
    const fixture = render();
    expect(statusText(fixture)).toBe('');

    events.next(new NavigationStart(1, '/abwab'));
    vi.advanceTimersByTime(50);
    events.next(new NavigationEnd(1, '/abwab', '/abwab'));
    vi.advanceTimersByTime(1000);
    expect(statusText(fixture)).toBe('');

    events.next(new NavigationStart(2, '/abwab'));
    vi.advanceTimersByTime(200);
    expect(statusText(fixture)).toBe('جارٍ تحميل الصفحة…');

    events.next(new NavigationEnd(2, '/abwab', '/abwab'));
    vi.advanceTimersByTime(400);
    expect(statusText(fixture)).toBe('');
  });

  it('keeps a single permanent polite status region and never renders a focusable element', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const statusRegions = root.querySelectorAll('[role="status"]');
    expect(statusRegions).toHaveLength(1);
    expect(statusRegions[0].classList.contains('qd-sr-only')).toBe(true);
    expect(root.querySelector('button, a, input, [tabindex]')).toBeNull();
  });
});
