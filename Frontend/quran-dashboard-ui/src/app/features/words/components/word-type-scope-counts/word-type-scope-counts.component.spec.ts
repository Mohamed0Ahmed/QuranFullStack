import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { WritableSignal, signal } from '@angular/core';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WORD_TYPE_TABLE_VIEW_OPTIONS } from '../../models/word-types.labels';
import { WordTypesScopeCountsState } from '../../models/word-types.models';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import { WordTypeScopeCountsComponent } from './word-type-scope-counts.component';

describe('WordTypeScopeCountsComponent', () => {
  let state: WritableSignal<WordTypesScopeCountsState>;
  let retryScopeCounts: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getTestBed().resetTestingModule();
    state = signal<WordTypesScopeCountsState>({ status: 'idle', counts: null });
    retryScopeCounts = vi.fn();
    const facadeStub = {
      scopeCountsState: state.asReadonly(),
      retryScopeCounts,
    } as unknown as WordTypesExplorerFacade;

    TestBed.configureTestingModule({
      imports: [WordTypeScopeCountsComponent],
      providers: [{ provide: WordTypesExplorerFacade, useValue: facadeStub }],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(tableFailed = false): HTMLElement {
    const fixture = TestBed.createComponent(WordTypeScopeCountsComponent);
    fixture.componentRef.setInput('tableFailed', tableFailed);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders nothing before a scope is confirmed (idle)', () => {
    const root = render();
    expect(root.textContent?.trim()).toBe('');
    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
  });

  it('renders four counts reusing the view tabs SHORT labels verbatim, in tab order', () => {
    state.set({ status: 'success', counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 } });
    const root = render();

    const labels = Array.from(root.querySelectorAll('.word-type-scope-counts__label')).map((el) => el.textContent?.trim());
    const values = Array.from(root.querySelectorAll('[data-testid="word-type-scope-count-value"]')).map((el) => el.textContent?.trim());

    // Order and text identical to the tabs (كلمات | جذور | أصول | صيغ) — the tabs are not renamed.
    expect(labels).toEqual(WORD_TYPE_TABLE_VIEW_OPTIONS.map((option) => option.label));
    expect(labels).toEqual(['كلمات', 'جذور', 'أصول', 'صيغ']);
    expect(values).toEqual(['40', '12', '8', '5']);
    // Non-interactive.
    expect(root.querySelector('button')).toBeNull();
  });

  it('renders zeros for an all-zero scope', () => {
    state.set({ status: 'success', counts: { wordsCount: 0, rootsCount: 0, stemsCount: 0, lemmasCount: 0 } });
    const root = render();

    const values = Array.from(root.querySelectorAll('[data-testid="word-type-scope-count-value"]')).map((el) => el.textContent?.trim());
    expect(values).toEqual(['0', '0', '0', '0']);
  });

  it('renders a non-interactive skeleton while loading, announced via sr-only text', () => {
    state.set({ status: 'loading', counts: null });
    const root = render();

    const skeleton = root.querySelector('[data-testid="word-type-scope-counts-skeleton"]');
    expect(skeleton).toBeTruthy();
    // The role="status" container must NOT be aria-hidden (that would nullify the announcement); the
    // visual skeleton bars are hidden while the sr-only loading text carries the announcement.
    expect(skeleton?.getAttribute('role')).toBe('status');
    expect(skeleton?.getAttribute('aria-hidden')).toBeNull();
    expect(skeleton?.querySelector('.qd-sr-only')?.textContent?.trim()).toBeTruthy();
    for (const bar of Array.from(skeleton!.querySelectorAll('.qd-skeleton'))) {
      expect(bar.getAttribute('aria-hidden')).toBe('true');
    }
    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
  });

  it('renders a compact error with a retry that refetches counts only', () => {
    state.set({ status: 'error', counts: null });
    const root = render();

    const retry = root.querySelector<HTMLButtonElement>('[data-testid="word-type-scope-counts-retry"]');
    expect(root.querySelector('[data-testid="word-type-scope-counts-error"]')).toBeTruthy();
    expect(retry).toBeTruthy();

    retry!.click();
    expect(retryScopeCounts).toHaveBeenCalledTimes(1);
  });

  it('hides the numbers when the table failed even if counts succeeded (scope unconfirmed)', () => {
    state.set({ status: 'success', counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 } });
    const root = render(true);

    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
    expect(root.textContent?.trim()).toBe('');
  });
});
