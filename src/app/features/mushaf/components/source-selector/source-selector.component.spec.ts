import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { SourceSelectorComponent } from './source-selector.component';
import { SourceOption } from '../../models/mushaf.models';

const arabicTafsir: SourceOption = {
  key: 'ar-muyassar',
  label: 'التفسير الميسر',
  languageCode: 'ar',
  languageNameAr: 'العربية',
};

const englishSahih: SourceOption = {
  key: 'en-sahih',
  label: 'صحيح الدولية',
  languageCode: 'en',
  languageNameAr: 'الإنجليزية',
};

const englishHaleem: SourceOption = {
  key: 'en-haleem',
  label: 'هيليم',
  languageCode: 'en',
  languageNameAr: 'الإنجليزية',
};

const i3rabSources: SourceOption[] = [
  { key: 'i3rab-1', label: 'مصدر إعراب ١', languageCode: 'ar', languageNameAr: 'العربية' },
  { key: 'i3rab-2', label: 'مصدر إعراب ٢', languageCode: 'ar', languageNameAr: 'العربية' },
  { key: 'i3rab-3', label: 'مصدر إعراب ٣', languageCode: 'ar', languageNameAr: 'العربية' },
  { key: 'i3rab-4', label: 'مصدر إعراب ٤', languageCode: 'ar', languageNameAr: 'العربية' },
];

function createFixture(
  inputs: Partial<{
    label: string;
    selectedKey: string | null;
    options: SourceOption[];
    pickerMode: 'languageFirst' | 'flat';
  }> = {},
) {
  const fixture = TestBed.createComponent(SourceSelectorComponent);
  fixture.componentRef.setInput('label', inputs.label ?? 'مصدر التفسير');
  fixture.componentRef.setInput('selectedKey', inputs.selectedKey ?? null);
  fixture.componentRef.setInput('options', inputs.options ?? [arabicTafsir, englishSahih, englishHaleem]);
  if (inputs.pickerMode) {
    fixture.componentRef.setInput('pickerMode', inputs.pickerMode);
  }
  fixture.detectChanges();
  return fixture;
}

function query<T extends HTMLElement>(root: ParentNode, testId: string): T {
  return root.querySelector(`[data-testid="${testId}"]`) as T;
}

function queryAll<T extends HTMLElement>(root: ParentNode, testId: string): T[] {
  return Array.from(root.querySelectorAll(`[data-testid="${testId}"]`)) as T[];
}

describe('SourceSelectorComponent', () => {
  it('shows the selected source label on the trigger after selection', () => {
    const fixture = createFixture({ selectedKey: 'ar-muyassar' });
    const triggerLabel = query<HTMLElement>(fixture.nativeElement, 'source-selector-trigger-label');

    expect(triggerLabel.textContent?.trim()).toBe('التفسير الميسر');
  });

  it('languageFirst: opens languages, filters by search, then emits and closes on source pick', () => {
    const fixture = createFixture({ pickerMode: 'languageFirst' });
    const sourceChange = vi.fn();
    fixture.componentInstance.sourceChange.subscribe(sourceChange);

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-trigger').click();
    fixture.detectChanges();

    expect(query(fixture.nativeElement, 'source-selector-panel')).toBeTruthy();
    expect(queryAll(fixture.nativeElement, 'source-selector-language-row')).toHaveLength(2);
    expect(queryAll(fixture.nativeElement, 'source-selector-source-row')).toHaveLength(0);

    const search = query<HTMLInputElement>(fixture.nativeElement, 'source-selector-language-search');
    search.value = 'انجل';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(queryAll(fixture.nativeElement, 'source-selector-language-row')).toHaveLength(1);

    queryAll<HTMLButtonElement>(fixture.nativeElement, 'source-selector-language-row')[0].click();
    fixture.detectChanges();

    const sourceRows = queryAll<HTMLButtonElement>(fixture.nativeElement, 'source-selector-source-row');
    expect(sourceRows).toHaveLength(2);
    sourceRows[0].click();
    fixture.detectChanges();

    expect(sourceChange).toHaveBeenCalledWith('en-sahih');
    expect(query(fixture.nativeElement, 'source-selector-panel')).toBeFalsy();
  });

  it('languageFirst: reopens directly to the selected source language', () => {
    const fixture = createFixture({
      pickerMode: 'languageFirst',
      selectedKey: 'en-sahih',
    });

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-trigger').click();
    fixture.detectChanges();

    expect(query(fixture.nativeElement, 'source-selector-language-search')).toBeFalsy();
    expect(queryAll(fixture.nativeElement, 'source-selector-source-row')).toHaveLength(2);
    expect(query(fixture.nativeElement, 'source-selector-back')).toBeTruthy();
  });

  it('languageFirst: back button returns to the languages step', () => {
    const fixture = createFixture({
      pickerMode: 'languageFirst',
      selectedKey: 'en-sahih',
    });

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-trigger').click();
    fixture.detectChanges();

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-back').click();
    fixture.detectChanges();

    expect(query(fixture.nativeElement, 'source-selector-language-search')).toBeTruthy();
    expect(queryAll(fixture.nativeElement, 'source-selector-language-row')).toHaveLength(2);
  });

  it('languageFirst: filters sources inside the selected language', () => {
    const fixture = createFixture({ pickerMode: 'languageFirst' });

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-trigger').click();
    fixture.detectChanges();

    queryAll<HTMLButtonElement>(fixture.nativeElement, 'source-selector-language-row')[1].click();
    fixture.detectChanges();

    const search = query<HTMLInputElement>(fixture.nativeElement, 'source-selector-source-search');
    search.value = 'هيليم';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(queryAll(fixture.nativeElement, 'source-selector-source-row')).toHaveLength(1);
    expect(query<HTMLElement>(fixture.nativeElement, 'source-selector-source-row').textContent).toContain('هيليم');
  });

  it('flat: opens directly to the source list with search', () => {
    const fixture = createFixture({ pickerMode: 'flat', options: i3rabSources });
    const sourceChange = vi.fn();
    fixture.componentInstance.sourceChange.subscribe(sourceChange);

    query<HTMLButtonElement>(fixture.nativeElement, 'source-selector-trigger').click();
    fixture.detectChanges();

    expect(query(fixture.nativeElement, 'source-selector-language-search')).toBeFalsy();
    expect(query(fixture.nativeElement, 'source-selector-source-search')).toBeTruthy();
    const sourceRows = queryAll(fixture.nativeElement, 'source-selector-source-row');
    expect(sourceRows).toHaveLength(4);

    sourceRows[2].click();
    fixture.detectChanges();

    expect(sourceChange).toHaveBeenCalledWith('i3rab-3');
    expect(query(fixture.nativeElement, 'source-selector-panel')).toBeFalsy();
  });

  it('renders a single static label when only one option exists', () => {
    const fixture = createFixture({ options: [arabicTafsir] });

    expect(query(fixture.nativeElement, 'source-selector-trigger')).toBeFalsy();
    expect(query<HTMLElement>(fixture.nativeElement, 'source-single-option').textContent?.trim()).toBe(
      'التفسير الميسر',
    );
  });
});
