import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { StemWordsListComponent } from './stem-words-list.component';
import { StemWordItemDto } from '../../models/stems.models';

function wordItem(uniqueWordId: number): StemWordItemDto {
  return {
    uniqueWordId,
    displayText: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
  };
}

describe('StemWordsListComponent', () => {
  it.each([
    { wordView: 'simple' as const, uniqueWordId: 1003 },
    { wordView: 'tashkeel' as const, uniqueWordId: 2003 },
  ])('builds the correct unique-words deep link for $wordView rows', async ({ wordView, uniqueWordId }) => {
    await TestBed.configureTestingModule({
      imports: [StemWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(StemWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(uniqueWordId)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', wordView);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="stem-word-link"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain(`/dashboard/words/unique/${wordView}`);
    expect(link.getAttribute('href')).toContain(`word=${uniqueWordId}`);
    expect(link.getAttribute('href')).toContain('view=ayahs');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders without a nested tablist', async () => {
    await TestBed.configureTestingModule({
      imports: [StemWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(StemWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(1)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="tablist"]')).toBeNull();
  });

  it('renders scoped counts, exact unique-word anchors, and pagination changes', async () => {
    await TestBed.configureTestingModule({
      imports: [StemWordsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(StemWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 1,
      totalCount: 2,
      items: [wordItem(1003)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="stem-word-link"]') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('/dashboard/words/unique/simple');
    expect(link.getAttribute('href')).toContain('word=1003');
    expect(link.getAttribute('href')).not.toContain('كلمة');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
    expect(fixture.nativeElement.querySelector('.qd-badge')?.textContent?.trim()).toBe('2');

    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    const nextButton = fixture.nativeElement.querySelector('[data-testid="qd-pagination-next"]') as HTMLButtonElement;
    nextButton?.click();

    expect(emitted).toEqual([2]);
  });
});
