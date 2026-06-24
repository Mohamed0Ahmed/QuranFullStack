import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootWordsListComponent } from './root-words-list.component';
import { RootWordItemDto, RootWordView } from '../../models/roots.models';

function wordItem(uniqueWordId: number, kind: RootWordView): RootWordItemDto {
  return {
    uniqueWordId,
    kind,
    displayTextUthmani: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
    firstVerseKey: '1:1',
  };
}

describe('RootWordsListComponent US3', () => {
  it.each([
    { wordView: 'simple' as const, uniqueWordId: 1003 },
    { wordView: 'tashkeel' as const, uniqueWordId: 2003 },
  ])('builds the correct unique-words deep link for $wordView rows', async ({ wordView, uniqueWordId }) => {
    await TestBed.configureTestingModule({
      imports: [RootWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(RootWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(uniqueWordId, wordView)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', wordView);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="root-word-link"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain(`/dashboard/words/unique/${wordView}`);
    expect(link.getAttribute('href')).toContain(`word=${uniqueWordId}`);
    expect(link.getAttribute('href')).toContain('view=ayahs');
  });
});
