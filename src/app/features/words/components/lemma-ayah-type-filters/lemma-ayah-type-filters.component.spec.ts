import { afterEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { LemmaAyahTypeFiltersComponent } from './lemma-ayah-type-filters.component';

describe('LemmaAyahTypeFiltersComponent', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders all filter selected by default and emits typeCode changes', async () => {
    await TestBed.configureTestingModule({
      imports: [LemmaAyahTypeFiltersComponent],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
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
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-all"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.textContent).toContain('عرض الكل');
    expect(root.textContent).toContain('اسم');
    expect(root.textContent).toContain('10 مرة');

    let emitted: string | null | undefined;
    fixture.componentInstance.typeCodeChange.subscribe((value) => (emitted = value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-N"]') as HTMLButtonElement).click();
    expect(emitted).toBe('N');
  });

  it('shows loading status when chips are not yet ready', async () => {
    await TestBed.configureTestingModule({
      imports: [LemmaAyahTypeFiltersComponent],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')).toBeTruthy();
  });
});
