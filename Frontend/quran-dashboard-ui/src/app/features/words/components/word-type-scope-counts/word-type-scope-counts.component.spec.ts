import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';

import { WORD_TYPE_TABLE_VIEW_OPTIONS } from '../../models/word-types.labels';
import { WordTypesScopeCountsState } from '../../models/word-types.models';
import { WordTypeScopeCountsComponent } from './word-type-scope-counts.component';

describe('WordTypeScopeCountsComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [WordTypeScopeCountsComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(
    state: WordTypesScopeCountsState,
    tableFailed = false,
  ): ComponentFixture<WordTypeScopeCountsComponent> {
    const fixture = TestBed.createComponent(WordTypeScopeCountsComponent);
    fixture.componentRef.setInput('state', state);
    fixture.componentRef.setInput('tableFailed', tableFailed);
    fixture.detectChanges();
    return fixture;
  }

  it('renders nothing before a scope is confirmed (idle)', () => {
    const root = render({ status: 'idle', counts: null }).nativeElement as HTMLElement;
    expect(root.textContent?.trim()).toBe('');
    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
  });

  const states: readonly WordTypesScopeCountsState[] = [
    { status: 'idle', counts: null },
    { status: 'loading', counts: null },
    { status: 'error', counts: null },
    { status: 'success', counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 } },
  ];

  it.each(states.map((state) => [state.status, state] as const))(
    'keeps the invisible metric mirror of the loaded item box in the %s state',
    (_status, state) => {
      const root = render(state).nativeElement as HTMLElement;

      const mirror = root.querySelector('.word-type-scope-counts--metric');
      expect(mirror).toBeTruthy();
      expect(mirror?.getAttribute('aria-hidden')).toBe('true');
      const mirrorItems = mirror!.querySelectorAll('.word-type-scope-counts__item');
      expect(mirrorItems.length).toBe(WORD_TYPE_TABLE_VIEW_OPTIONS.length);
      for (const item of Array.from(mirrorItems)) {
        expect(item.querySelector('.word-type-scope-counts__line--label')).toBeTruthy();
        expect(item.querySelector('.word-type-scope-counts__line--value')).toBeTruthy();
      }
      expect(mirror!.querySelectorAll('.qd-skeleton').length).toBe(0);
      expect(mirror!.querySelector('[data-testid]')).toBeNull();
    },
  );

  it('keeps the metric mirror reserved when the table failed and the numbers are hidden', () => {
    const root = render(
      { status: 'success', counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 } },
      true,
    ).nativeElement as HTMLElement;

    expect(root.querySelectorAll('.word-type-scope-counts--metric .word-type-scope-counts__item').length)
      .toBe(WORD_TYPE_TABLE_VIEW_OPTIONS.length);
  });

  it('renders four counts reusing the view tabs SHORT labels verbatim, in tab order', () => {
    const root = render({
      status: 'success',
      counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 },
    }).nativeElement as HTMLElement;

    const labels = Array.from(root.querySelectorAll('.word-type-scope-counts__label')).map((el) => el.textContent?.trim());
    const values = Array.from(root.querySelectorAll('[data-testid="word-type-scope-count-value"]')).map((el) => el.textContent?.trim());

    expect(labels).toEqual(WORD_TYPE_TABLE_VIEW_OPTIONS.map((option) => option.label));
    expect(labels).toEqual(['كلمات', 'جذور', 'أصول', 'صيغ']);
    expect(values).toEqual(['40', '12', '8', '5']);
    expect(root.querySelector('button')).toBeNull();
  });

  it('renders zeros for an all-zero scope', () => {
    const root = render({
      status: 'success',
      counts: { wordsCount: 0, rootsCount: 0, stemsCount: 0, lemmasCount: 0 },
    }).nativeElement as HTMLElement;

    const values = Array.from(root.querySelectorAll('[data-testid="word-type-scope-count-value"]')).map((el) => el.textContent?.trim());
    expect(values).toEqual(['0', '0', '0', '0']);
  });

  it('renders a non-interactive skeleton while loading, announced via sr-only text', () => {
    const root = render({ status: 'loading', counts: null }).nativeElement as HTMLElement;

    const skeleton = root.querySelector('[data-testid="word-type-scope-counts-skeleton"]');
    expect(skeleton).toBeTruthy();
    expect(skeleton?.getAttribute('role')).toBe('status');
    expect(skeleton?.getAttribute('aria-busy')).toBe('true');
    expect(skeleton?.getAttribute('aria-hidden')).toBeNull();
    expect(skeleton?.querySelector('.qd-sr-only')?.textContent?.trim()).toBeTruthy();
    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
  });

  it('mirrors the loaded two-line item box while loading, so the strip keeps its height', () => {
    const loading = render({ status: 'loading', counts: null }).nativeElement as HTMLElement;
    const loaded = render({
      status: 'success',
      counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 },
    }).nativeElement as HTMLElement;

    const skeleton = loading.querySelector('[data-testid="word-type-scope-counts-skeleton"]')!;
    const skeletonItems = skeleton.querySelectorAll('.word-type-scope-counts__item');
    const loadedItems = loaded.querySelectorAll('[data-testid="word-type-scope-counts"] .word-type-scope-counts__item');

    expect(skeletonItems.length).toBe(loadedItems.length);
    expect(skeletonItems.length).toBe(WORD_TYPE_TABLE_VIEW_OPTIONS.length);

    for (const item of Array.from(skeletonItems)) {
      expect(item.getAttribute('aria-hidden')).toBe('true');
      const label = item.querySelector('.word-type-scope-counts__line--label');
      const value = item.querySelector('.word-type-scope-counts__line--value');
      expect(label?.classList.contains('qd-skeleton')).toBe(true);
      expect(value?.classList.contains('qd-skeleton')).toBe(true);
    }
  });

  it('renders a compact error (role="alert") whose retry emits retryRequested for a counts-only refetch', () => {
    const fixture = render({ status: 'error', counts: null });
    const root = fixture.nativeElement as HTMLElement;
    let retries = 0;
    fixture.componentInstance.retryRequested.subscribe(() => (retries += 1));

    const error = root.querySelector('[data-testid="word-type-scope-counts-error"]');
    const retry = root.querySelector<HTMLButtonElement>('[data-testid="word-type-scope-counts-retry"]');
    expect(error).toBeTruthy();
    expect(error?.getAttribute('role')).toBe('alert');
    expect(retry).toBeTruthy();

    retry!.click();
    expect(retries).toBe(1);
  });

  it('hides the numbers when the table failed even if counts succeeded (scope unconfirmed)', () => {
    const root = render(
      { status: 'success', counts: { wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 } },
      true,
    ).nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="word-type-scope-counts"]')).toBeNull();
    expect(root.textContent?.trim()).toBe('');
  });
});
