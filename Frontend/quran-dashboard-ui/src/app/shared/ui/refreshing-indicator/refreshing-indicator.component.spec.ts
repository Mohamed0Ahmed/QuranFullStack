import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdRefreshingIndicatorComponent } from './refreshing-indicator.component';

describe('QdRefreshingIndicatorComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdRefreshingIndicatorComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(active = false) {
    const fixture = TestBed.createComponent(QdRefreshingIndicatorComponent);
    fixture.componentRef.setInput('active', active);
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement };
  }

  it('adds nothing to the document while idle', () => {
    const { root } = render();

    expect(root.querySelector('.qd-refreshing')).toBeNull();
    expect(root.textContent?.trim()).toBe('');
  });

  it('shows one solid segment on one flat track while refreshing', () => {
    const { root } = render(true);
    const track = root.querySelector('[data-testid="qd-refreshing-indicator"]');

    expect(track).toBeTruthy();
    expect(track?.querySelectorAll('.qd-refreshing__segment')).toHaveLength(1);
  });

  // The refresh treatment is decoration over content that stays readable: the region it sits
  // on carries `aria-busy`, so the indicator itself must not announce or trap anything.
  it('announces nothing itself: no status, alert, dialog role or live region', () => {
    const { root } = render(true);
    const track = root.querySelector('[data-testid="qd-refreshing-indicator"]') as HTMLElement;

    expect(track.getAttribute('aria-hidden')).toBe('true');
    expect(track.getAttribute('role')).toBeNull();
    expect(track.getAttribute('aria-live')).toBeNull();
    expect(root.querySelector('[role], [aria-live], [aria-busy]')).toBeNull();
  });

  it('unmounts the track when the refresh settles, leaving no residual geometry', () => {
    const { fixture, root } = render(true);

    fixture.componentRef.setInput('active', false);
    fixture.detectChanges();

    expect(root.querySelector('.qd-refreshing')).toBeNull();
  });
});
