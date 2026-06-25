import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AyahMatchesListComponent } from './ayah-matches-list.component';
import { PagedResultDto, UniqueWordAyahMatchDto } from '../../models/unique-words.models';

const PAGE: PagedResultDto<UniqueWordAyahMatchDto> = {
  page: 1,
  pageSize: 10,
  totalCount: 1,
  items: [
    {
      ayahId: 7001,
      verseKey: '4:57',
      surahNumber: 4,
      surahNameArabic: 'النساء',
      ayahNumber: 57,
      pageNumber: 92,
      matchedQuranWordIds: [9001],
      words: [
        {
          quranWordId: 9001,
          wordNumber: 1,
          textUthmani: 'كلمة-تجريبية-١',
          isAyahMarker: false,
        },
      ],
    },
  ],
};

function setInputs(
  fixture: ComponentFixture<AyahMatchesListComponent>,
  inputs: { page: PagedResultDto<UniqueWordAyahMatchDto>; currentPage: number },
): void {
  fixture.componentRef.setInput('page', inputs.page);
  fixture.componentRef.setInput('currentPage', inputs.currentPage);
  fixture.detectChanges();
}

describe('AyahMatchesListComponent', () => {
  it('opens the matching ayah in Mushaf in a new tab when the ayah text is clicked', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    setInputs(fixture, { page: PAGE, currentPage: 1 });

    const root = fixture.nativeElement as HTMLElement;
    const actionLink = root.querySelector(
      '[data-testid="ayah-matches-open-mushaf"]',
    ) as HTMLAnchorElement | null;

    expect(actionLink?.getAttribute('href')).toBe(
      '/dashboard/mushaf?page=92&ayah=4:57&focusAyah=4:57&panel=ayah',
    );
    expect(actionLink?.getAttribute('aria-label')).toBe('فتح الآية في المصحف');
    expect(actionLink?.getAttribute('target')).toBe('_blank');
    expect(actionLink?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(actionLink?.querySelector('[data-testid="highlighted-ayah"]')).not.toBeNull();
  });
});
