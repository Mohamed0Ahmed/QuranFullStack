import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { MushafSurahJuzGroupDto } from '../../models/mushaf.models';
import { SurahJumpPickerComponent } from './surah-jump-picker.component';

const catalogFixture: readonly MushafSurahJuzGroupDto[] = [
  {
    juzNumber: 30,
    surahs: [
      { surahNumber: 101, nameArabic: 'سورة-تجريبية-١', startPageNumber: 600 },
      { surahNumber: 102, nameArabic: 'سورة-تجريبية-٢', startPageNumber: 601 },
    ],
  },
];

describe('SurahJumpPickerComponent', () => {
  it('renders one chevron and opens the search panel', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelectorAll('.surah-jump-picker__trigger-chevron')).toHaveLength(1);
    expect(root.querySelector('[data-testid="surah-jump-picker-panel"]')).toBeNull();

    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="surah-jump-picker-panel"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="surah-jump-picker-search"]')).toBeTruthy();
    expect(root.querySelector('#surah-jump-listbox')).toBeTruthy();
  });

  it('does not open when the catalog is empty', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', []);
    fixture.detectChanges();

    const trigger = fixture.nativeElement.querySelector(
      '[data-testid="surah-jump-picker-trigger"]',
    ) as HTMLButtonElement;

    expect(trigger.disabled).toBe(true);
    trigger.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="surah-jump-picker-panel"]')).toBeNull();
  });

  it('shows juz groups when browsing and flat results when searching', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(root.querySelectorAll('.surah-jump-picker__group-heading')).toHaveLength(1);

    const search = root.querySelector('[data-testid="surah-jump-picker-search"]') as HTMLInputElement;
    search.value = 'تجريبية-٢';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(root.querySelector('.surah-jump-picker__group-heading')).toBeNull();
    expect(root.querySelectorAll('[data-testid="surah-jump-picker-row"]')).toHaveLength(1);
  });

  it('shows an empty state when search has no matches', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const search = root.querySelector('[data-testid="surah-jump-picker-search"]') as HTMLInputElement;
    search.value = 'missing';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(root.textContent).toContain('لا توجد سورة مطابقة.');
    expect(root.querySelectorAll('[data-testid="surah-jump-picker-row"]')).toHaveLength(0);
  });

  it('filters by surah number in search mode', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const search = root.querySelector('[data-testid="surah-jump-picker-search"]') as HTMLInputElement;
    search.value = '102';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const rows = root.querySelectorAll('[data-testid="surah-jump-picker-row"]');
    expect(rows).toHaveLength(1);
    expect(rows[0].textContent?.trim()).toBe('102. سورة-تجريبية-٢');
  });

  it('emits surahJump when a row is selected', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const emitSpy = vi.fn();
    fixture.componentInstance.surahJump.subscribe(emitSpy);

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    (root.querySelector('[data-testid="surah-jump-picker-row"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledWith(101);
    expect(root.querySelector('[data-testid="surah-jump-picker-panel"]')).toBeNull();
  });

  it('closes on Escape and restores focus to the trigger', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector(
      '[data-testid="surah-jump-picker-trigger"]',
    ) as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="surah-jump-picker-panel"]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
  });

  it('moves the active option with arrow keys', () => {
    const fixture = TestBed.createComponent(SurahJumpPickerComponent);
    fixture.componentRef.setInput('surahCatalogByJuz', catalogFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="surah-jump-picker-trigger"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const listbox = root.querySelector('#surah-jump-listbox') as HTMLElement;
    expect(listbox.getAttribute('aria-activedescendant')).toBe('surah-jump-option-j30-s101');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();

    expect(listbox.getAttribute('aria-activedescendant')).toBe('surah-jump-option-j30-s102');
  });
});
