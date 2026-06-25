import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { LemmaWordsListComponent } from './lemma-words-list.component';
import { LemmaWordItemDto } from '../../models/lemmas.models';

function wordItem(uniqueWordId: number, kind: 'simple' | 'tashkeel'): LemmaWordItemDto {
  return {
    uniqueWordId,
    kind,
    displayTextUthmani: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
    firstVerseKey: '1:1',
  };
}

describe('LemmaWordsListComponent', () => {
  it.each([
    { wordView: 'simple' as const, uniqueWordId: 1003 },
    { wordView: 'tashkeel' as const, uniqueWordId: 2003 },
  ])('builds the correct unique-words deep link for $wordView rows', async ({ wordView, uniqueWordId }) => {
    await TestBed.configureTestingModule({
      imports: [LemmaWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(uniqueWordId, wordView)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', wordView);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="lemma-word-link"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain(`/dashboard/words/unique/${wordView}`);
    expect(link.getAttribute('href')).toContain(`word=${uniqueWordId}`);
    expect(link.getAttribute('href')).toContain('view=ayahs');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('emits word-view changes from the nested tabs', async () => {
    await TestBed.configureTestingModule({
      imports: [LemmaWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', { page: 1, pageSize: 100, totalCount: 1, items: [wordItem(1, 'simple')] });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    const emitted: string[] = [];
    fixture.componentInstance.wordViewChange.subscribe((value) => emitted.push(value));

    const tashkeelTab = fixture.nativeElement.querySelector(
      '[data-testid="lemma-words-tab-tashkeel"]',
    ) as HTMLButtonElement | null;
    tashkeelTab?.click();

    expect(emitted).toEqual(['tashkeel']);
  });

  it('keeps the pagination out of the loading state', async () => {
    await TestBed.configureTestingModule({
      imports: [LemmaWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('loading', true);
    fixture.componentRef.setInput('page', { page: 1, pageSize: 100, totalCount: 0, items: [] });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="lemma-words-list-loading"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('qd-pagination')).toBeNull();
  });
});
