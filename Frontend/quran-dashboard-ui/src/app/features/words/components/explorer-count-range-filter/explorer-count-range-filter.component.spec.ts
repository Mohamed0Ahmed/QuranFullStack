import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { ExplorerCountRangeFilterComponent } from './explorer-count-range-filter.component';
import { RangeFilters, RangeMetric } from '../../state/words-range-filters';

// occurrences takes its family's default threshold (100); surahs overrides its shared ayahsSurahs
// family down to 50, which is the split the component has to honor per metric rather than per family.
const METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: 'المواضع' },
  { key: 'surahs', urlKey: 'surahs', apiKey: 'surahs', family: 'ayahsSurahs', labelAr: 'السور', threshold: 50 },
];

describe('ExplorerCountRangeFilterComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ExplorerCountRangeFilterComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function render(ranges: RangeFilters, disabled = false) {
    const fixture = TestBed.createComponent(ExplorerCountRangeFilterComponent);
    fixture.componentRef.setInput('metrics', METRICS);
    fixture.componentRef.setInput('ranges', ranges);
    fixture.componentRef.setInput('disabled', disabled);
    fixture.detectChanges();
    return fixture;
  }

  function chipTextsOf(root: HTMLElement, metricKey: string): string[] {
    const chips = root.querySelectorAll(`[data-testid="range-filter-metric-${metricKey}"] .range-filter__chip`);
    return [...chips].map((chip) => chip.textContent?.trim() ?? '');
  }

  function inputOf(root: HTMLElement, bound: 'min' | 'max', metricKey: string): HTMLInputElement {
    return root.querySelector<HTMLInputElement>(`[data-testid="range-filter-${bound}-${metricKey}"]`)!;
  }

  // Mirrors a real keystroke: the browser updates the input, then Angular runs change detection.
  function type(fixture: ReturnType<typeof render>, input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function openCustom(fixture: ReturnType<typeof render>, metricKey: string): void {
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLButtonElement>(`[data-testid="range-filter-custom-${metricKey}"]`)!.click();
    fixture.detectChanges();
  }

  function press(input: HTMLInputElement, key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    input.dispatchEvent(event);
    return event;
  }

  describe('chips', () => {
    it('renders exactly three chips per metric, labelled with that metric’s own threshold', () => {
      const root = render({}).nativeElement as HTMLElement;

      expect(chipTextsOf(root, 'occurrences')).toEqual(['أكثر من 100', 'أقل من 100', 'مخصّص']);
      expect(chipTextsOf(root, 'surahs')).toEqual(['أكثر من 50', 'أقل من 50', 'مخصّص']);
      expect(
        root.querySelector('[data-testid="range-filter-chip-occurrences-gt"]')?.classList.contains('qd-action'),
      ).toBe(true);
    });

    it.each([
      { metricKey: 'occurrences', kind: 'gt', expected: { min: 101, max: null } },
      { metricKey: 'occurrences', kind: 'lt', expected: { min: null, max: 99 } },
      { metricKey: 'surahs', kind: 'gt', expected: { min: 51, max: null } },
      { metricKey: 'surahs', kind: 'lt', expected: { min: null, max: 49 } },
    ])('emits $metricKey $kind as a strict open-ended range', ({ metricKey, kind, expected }) => {
      const fixture = render({});
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      const chip = root.querySelector<HTMLButtonElement>(`[data-testid="range-filter-chip-${metricKey}-${kind}"]`)!;
      expect(chip.getAttribute('aria-pressed')).toBe('false');
      chip.click();

      expect(emitted).toEqual({ [metricKey]: expected });
    });

    it('marks the chip a committed range matches via aria-pressed', () => {
      const root = render({ surahs: { min: 51, max: null } }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="range-filter-chip-surahs-gt"]')!.getAttribute('aria-pressed')).toBe('true');
      expect(root.querySelector('[data-testid="range-filter-chip-surahs-lt"]')!.getAttribute('aria-pressed')).toBe('false');
    });

    it('re-clicking the active chip clears the metric', () => {
      const fixture = render({ occurrences: { min: 101, max: null } });
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));
      root.querySelector<HTMLButtonElement>('[data-testid="range-filter-chip-occurrences-gt"]')!.click();

      expect(emitted).toEqual({});
    });

    it('reopens a shared link carrying a non-chip range as an active مخصّص', () => {
      const root = render({ occurrences: { min: 11, max: 100 } }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="range-filter-custom-occurrences"]')!.getAttribute('aria-pressed')).toBe('true');
      expect(root.querySelector('[data-testid="range-filter-chip-occurrences-gt"]')!.getAttribute('aria-pressed')).toBe('false');
      expect(root.querySelector('[data-testid="range-filter-chip-occurrences-lt"]')!.getAttribute('aria-pressed')).toBe('false');
      expect(inputOf(root, 'min', 'occurrences').value).toBe('11');
      expect(inputOf(root, 'max', 'occurrences').value).toBe('100');
    });

    it('disables every chip while the list is loading', () => {
      const root = render({}, true).nativeElement as HTMLElement;

      const chips = root.querySelectorAll<HTMLButtonElement>('.range-filter__chip');
      expect(chips.length).toBe(6);
      expect([...chips].every((chip) => chip.disabled)).toBe(true);
    });
  });

  describe('مخصّص commit', () => {
    function openCustom(fixture: ReturnType<typeof render>, metricKey: string): void {
      const root = fixture.nativeElement as HTMLElement;
      root.querySelector<HTMLButtonElement>(`[data-testid="range-filter-custom-${metricKey}"]`)!.click();
      fixture.detectChanges();
    }

    it('does not emit while the user is typing', () => {
      const fixture = render({});
      openCustom(fixture, 'surahs');
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      type(fixture, inputOf(root, 'min', 'surahs'), '1');
      type(fixture, inputOf(root, 'min', 'surahs'), '15');
      type(fixture, inputOf(root, 'max', 'surahs'), '40');

      expect(emitted).toBeUndefined();
    });

    it('commits the draft on Enter', () => {
      const fixture = render({});
      openCustom(fixture, 'surahs');
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      type(fixture, inputOf(root, 'min', 'surahs'), '15');
      type(fixture, inputOf(root, 'max', 'surahs'), '40');
      const enter = press(inputOf(root, 'min', 'surahs'), 'Enter');

      expect(emitted).toEqual({ surahs: { min: 15, max: 40 } });
      expect(enter.defaultPrevented).toBe(true);
    });

    it('fails open by dropping the max when the committed draft has min greater than max', () => {
      const fixture = render({});
      openCustom(fixture, 'surahs');
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      type(fixture, inputOf(root, 'min', 'surahs'), '9');
      type(fixture, inputOf(root, 'max', 'surahs'), '2');
      press(inputOf(root, 'max', 'surahs'), 'Enter');

      expect(emitted).toEqual({ surahs: { min: 9, max: null } });
    });

    it('commits the draft from the تطبيق button', () => {
      const fixture = render({});
      openCustom(fixture, 'surahs');
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      type(fixture, inputOf(root, 'min', 'surahs'), '15');
      root.querySelector<HTMLButtonElement>('[data-testid="range-filter-apply-surahs"]')!.click();

      expect(emitted).toEqual({ surahs: { min: 15, max: null } });
    });

    it('reverts the draft to the committed range on Escape without emitting', () => {
      const fixture = render({ surahs: { min: 5, max: 40 } });
      const root = fixture.nativeElement as HTMLElement;

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      type(fixture, inputOf(root, 'min', 'surahs'), '9');
      press(inputOf(root, 'min', 'surahs'), 'Escape');
      fixture.detectChanges();

      expect(inputOf(root, 'min', 'surahs').value).toBe('5');
      expect(emitted).toBeUndefined();
    });

    it('re-syncs an uncommitted draft when the ranges change outside the component', () => {
      const fixture = render({ surahs: { min: 5, max: 40 } });
      const root = fixture.nativeElement as HTMLElement;

      type(fixture, inputOf(root, 'min', 'surahs'), '9');

      fixture.componentRef.setInput('ranges', { surahs: { min: 2, max: 30 } });
      fixture.detectChanges();

      expect(inputOf(root, 'min', 'surahs').value).toBe('2');
      expect(inputOf(root, 'max', 'surahs').value).toBe('30');
    });

    // The pages re-`set` a freshly parsed ranges object on every navigation, so a draft must survive
    // a sibling metric's commit — otherwise committing one metric silently wipes text the user is
    // still typing in another open panel.
    it('keeps an uncommitted draft when a different metric commits and the ranges object is replaced', () => {
      const fixture = render({});
      const root = fixture.nativeElement as HTMLElement;
      openCustom(fixture, 'occurrences');
      openCustom(fixture, 'surahs');

      type(fixture, inputOf(root, 'min', 'occurrences'), '150');
      type(fixture, inputOf(root, 'min', 'surahs'), '20');
      press(inputOf(root, 'min', 'surahs'), 'Enter');
      fixture.detectChanges();

      // The page navigates and feeds a new object back in; occurrences never committed.
      fixture.componentRef.setInput('ranges', { surahs: { min: 20, max: null } });
      fixture.detectChanges();

      expect(inputOf(root, 'min', 'occurrences').value).toBe('150');
    });

    it('does not commit on blur — the draft persists and nothing is emitted', () => {
      const fixture = render({ surahs: { min: 5, max: 40 } });
      const root = fixture.nativeElement as HTMLElement;
      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

      const min = inputOf(root, 'min', 'surahs');
      type(fixture, min, '9');
      min.dispatchEvent(new FocusEvent('blur', { bubbles: true }));
      fixture.detectChanges();

      expect(emitted).toBeUndefined();
      expect(inputOf(root, 'min', 'surahs').value).toBe('9');
    });
  });

  describe('clear all', () => {
    it('clears every active range', () => {
      const fixture = render({ occurrences: { min: 101, max: null }, surahs: { min: 51, max: null } });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="range-filter-active-count"]')?.textContent?.trim()).toBe('2');

      let emitted: RangeFilters | undefined;
      fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));
      root.querySelector<HTMLButtonElement>('[data-testid="range-filter-clear-all"]')!.click();

      expect(emitted).toEqual({});
    });

    it('resets the drafts once the cleared ranges come back in', () => {
      const fixture = render({ occurrences: { min: 5, max: null } });
      const root = fixture.nativeElement as HTMLElement;
      type(fixture, inputOf(root, 'min', 'occurrences'), '9');

      root.querySelector<HTMLButtonElement>('[data-testid="range-filter-clear-all"]')!.click();
      fixture.componentRef.setInput('ranges', {});
      fixture.detectChanges();

      root.querySelector<HTMLButtonElement>('[data-testid="range-filter-custom-occurrences"]')!.click();
      fixture.detectChanges();

      expect(inputOf(root, 'min', 'occurrences').value).toBe('');
    });
  });
});
