import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { ExplorerCountRangeFilterComponent } from './explorer-count-range-filter.component';
import { RangeFilters, RangeMetric } from '../../state/words-range-filters';

const METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: 'المواضع' },
  { key: 'surahs', urlKey: 'surahs', apiKey: 'surahs', family: 'ayahsSurahs', labelAr: 'السور' },
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

  it('renders a bucket-chip group per metric with the preset labels', () => {
    const fixture = render({});
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="range-filter-metric-occurrences"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="range-filter-bucket-occurrences-1001+"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="range-filter-bucket-surahs-51+"]')).toBeTruthy();
  });

  it('marks the active bucket via aria-pressed and emits the canonical range on toggle', () => {
    const fixture = render({});
    const root = fixture.nativeElement as HTMLElement;

    let emitted: RangeFilters | undefined;
    fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

    const bucket = root.querySelector<HTMLButtonElement>('[data-testid="range-filter-bucket-occurrences-11–100"]')!;
    expect(bucket.getAttribute('aria-pressed')).toBe('false');
    bucket.click();

    expect(emitted).toEqual({ occurrences: { min: 11, max: 100 } });
  });

  it('re-clicking the active bucket clears the metric', () => {
    const fixture = render({ occurrences: { min: 11, max: 100 } });
    const root = fixture.nativeElement as HTMLElement;

    const bucket = root.querySelector<HTMLButtonElement>('[data-testid="range-filter-bucket-occurrences-11–100"]')!;
    expect(bucket.getAttribute('aria-pressed')).toBe('true');

    let emitted: RangeFilters | undefined;
    fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));
    bucket.click();

    expect(emitted).toEqual({});
  });

  it('emits a custom min-only open range from the custom inputs', () => {
    const fixture = render({});
    const root = fixture.nativeElement as HTMLElement;

    root.querySelector<HTMLButtonElement>('[data-testid="range-filter-custom-surahs"]')!.click();
    fixture.detectChanges();

    let emitted: RangeFilters | undefined;
    fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));

    const min = root.querySelector<HTMLInputElement>('[data-testid="range-filter-min-surahs"]')!;
    min.value = '5';
    min.dispatchEvent(new Event('input'));

    expect(emitted).toEqual({ surahs: { min: 5, max: null } });
  });

  it('clears every active range via the clear-all control', () => {
    const fixture = render({ occurrences: { min: 1, max: 1 }, surahs: { min: 51, max: null } });
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="range-filter-active-count"]')?.textContent?.trim()).toBe('2');

    let emitted: RangeFilters | undefined;
    fixture.componentInstance.rangesChange.subscribe((value) => (emitted = value));
    root.querySelector<HTMLButtonElement>('[data-testid="range-filter-clear-all"]')!.click();

    expect(emitted).toEqual({});
  });

  it('disables every chip while the list is loading', () => {
    const fixture = render({}, true);
    const root = fixture.nativeElement as HTMLElement;

    const chips = root.querySelectorAll<HTMLButtonElement>('.range-filter__chip');
    expect(chips.length).toBeGreaterThan(0);
    expect([...chips].every((chip) => chip.disabled)).toBe(true);
  });
});
