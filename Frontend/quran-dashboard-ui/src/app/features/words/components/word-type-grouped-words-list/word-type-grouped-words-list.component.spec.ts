import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeGroupedWordsListComponent } from './word-type-grouped-words-list.component';
import { WordTypeGroupedMemberWordDto } from '../../models/word-types-detail.models';
import { PagedResultDto } from '../../models/word-types.models';

function member(overrides: Partial<WordTypeGroupedMemberWordDto> = {}): WordTypeGroupedMemberWordDto {
  return {
    tashkeelWordId: 191001,
    contextCode: 'PN',
    case: 'all',
    tense: 'all',
    voice: 'all',
    displayText: 'SYNTH_WORD_TEXT',
    typeCode: 'PN',
    typeLabel: { ar: 'SYNTH_TYPE_LABEL' },
    broadLabel: { ar: 'SYNTH_BROAD_LABEL' },
    caseOrFeature: null,
    rootText: 'SYNTH_ROOT_TEXT',
    lemmaText: null,
    stemText: null,
    occurrencesCount: 4,
    ayahsCount: 3,
    surahsCount: 2,
    ...overrides,
  };
}

function page(items: WordTypeGroupedMemberWordDto[], totalCount = items.length): PagedResultDto<WordTypeGroupedMemberWordDto> {
  return { page: 1, pageSize: 25, totalCount, items };
}

function createList(pageValue: PagedResultDto<WordTypeGroupedMemberWordDto>, currentPage = 1) {
  const fixture = TestBed.createComponent(WordTypeGroupedWordsListComponent);
  fixture.componentRef.setInput('page', pageValue);
  fixture.componentRef.setInput('currentPage', currentPage);
  fixture.detectChanges();
  return fixture;
}

describe('WordTypeGroupedWordsListComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [WordTypeGroupedWordsListComponent], teardown: { destroyAfterEach: true } });
  });

  afterEach(() => getTestBed().resetTestingModule());

  it('renders each member word context with its three scoped counts', () => {
    const fixture = createList(page([member()]));
    const host = fixture.nativeElement as HTMLElement;

    const row = host.querySelector('[data-testid="word-type-grouped-word-row"]') as HTMLElement;
    expect(row).not.toBeNull();
    expect(row.textContent).toContain('SYNTH_WORD_TEXT');
    expect(row.textContent).toContain('SYNTH_TYPE_LABEL');
    expect(row.textContent).toContain('4');
    expect(row.textContent).toContain('3');
    expect(row.textContent).toContain('2');
  });

  it('member rows contain no button, anchor, tabindex, interactive-surface, or selected class', () => {
    const fixture = createList(page([member(), member({ tashkeelWordId: 191002, contextCode: 'N' })]));
    const viewport = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="word-type-grouped-words-viewport"]',
    ) as HTMLElement;

    expect(viewport.querySelectorAll('button, a, [tabindex]')).toHaveLength(0);
    expect(viewport.querySelector('.qd-interactive-surface')).toBeNull();
    expect(viewport.querySelector('.qd-is-selected')).toBeNull();
    expect(viewport.querySelector('[aria-selected]')).toBeNull();
  });

  it('emits pagination as the only interaction', () => {
    const fixture = createList(page([member()], 60), 1);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    const nextPageButton = (fixture.nativeElement as HTMLElement).querySelector(
      'qd-pagination [data-testid="qd-pagination-page-2"]',
    ) as HTMLButtonElement;
    expect(nextPageButton).not.toBeNull();
    nextPageButton.click();

    expect(emitted).toEqual([2]);
  });

  it('keeps count labels in the markup so mobile rows stay readable in RTL', () => {
    const fixture = createList(page([member()]));
    const labels = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="word-type-grouped-word-row"] [data-label]'),
    ).map((cell) => cell.getAttribute('data-label'));

    expect(labels).toEqual(['المواضع', 'الآيات', 'السور']);
  });

  // N3 row 4: the pagination bar unmounted while loading and the panel body sprang up under it.
  it('keeps the pagination slot rendered while loading and once loaded', () => {
    const fixture = createList(page([member()]));
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;

    const slot = () => host.querySelector('[data-testid="word-type-grouped-words-pagination-slot"]');
    expect(slot()).toBeTruthy();
    // While loading the slot is deliberately empty — it reserves the row, it does not render the bar.
    expect(slot()!.querySelector('qd-pagination')).toBeNull();

    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
    expect(slot()).toBeTruthy();
    expect(slot()!.querySelector('qd-pagination')).toBeTruthy();
  });
});
