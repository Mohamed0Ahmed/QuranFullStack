import { afterEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { TypeDistributionListComponent } from './type-distribution-list.component';

describe('TypeDistributionListComponent US6', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders the ordered distribution with a non-color dominant marker', async () => {
    await TestBed.configureTestingModule({
      imports: [TypeDistributionListComponent],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(TypeDistributionListComponent);
    fixture.componentRef.setInput('items', [
      {
        code: 'N',
        arabicLabel: 'اسم',
        englishLabel: 'Noun',
        occurrencesCount: 10,
        firstSurahNumber: 1,
        firstAyahNumber: 1,
        firstWordNumber: 1,
      },
      {
        code: 'V',
        arabicLabel: 'فعل',
        englishLabel: 'Verb',
        occurrencesCount: 1,
        firstSurahNumber: 1,
        firstAyahNumber: 2,
        firstWordNumber: 1,
      },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const rows = root.querySelectorAll('[data-testid="type-distribution-item"], [data-testid="type-distribution-dominant"]');

    expect(rows).toHaveLength(2);
    expect(root.querySelector('[data-testid="type-distribution-dominant"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="type-distribution-dominant"]')?.getAttribute('aria-current')).toBe('true');
    expect(root.textContent).toContain('اسم');
    expect(root.textContent).toContain('10');
    expect(root.textContent).toContain('فعل');
    expect(root.textContent).toContain('1');
  });
});
