import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeDetailSummaryComponent } from './word-type-detail-summary.component';
import { WORD_TYPES_TABLE_HEADERS } from '../../models/word-types.labels';

describe('WordTypeDetailSummaryComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [WordTypeDetailSummaryComponent], teardown: { destroyAfterEach: true } });
  });

  afterEach(() => getTestBed().resetTestingModule());

  it('renders the selection label and the three scoped measures with their values', () => {
    const fixture = TestBed.createComponent(WordTypeDetailSummaryComponent);
    fixture.componentRef.setInput('label', 'ك ل م');
    fixture.componentRef.setInput('occurrences', 3);
    fixture.componentRef.setInput('ayahs', 2);
    fixture.componentRef.setInput('surahs', 1);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const summary = host.querySelector('[data-testid="word-type-detail-summary"]');
    expect(summary).not.toBeNull();
    expect(host.querySelector('[data-testid="word-type-detail-summary-label"]')?.textContent).toContain('ك ل م');

    const measures = Array.from(host.querySelectorAll('[data-testid="word-type-detail-summary-measure"]')).map(
      (measure) => ({
        label: measure.querySelector('.word-type-detail-summary__measure-label')?.textContent?.trim(),
        value: measure.querySelector('.word-type-detail-summary__measure-value')?.textContent?.trim(),
      }),
    );
    expect(measures).toEqual([
      { label: WORD_TYPES_TABLE_HEADERS.occurrences, value: '3' },
      { label: WORD_TYPES_TABLE_HEADERS.ayahs, value: '2' },
      { label: WORD_TYPES_TABLE_HEADERS.surahs, value: '1' },
    ]);
  });
});
