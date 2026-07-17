import { beforeEach, describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { MushafLineComponent } from './mushaf-line.component';
import { MUSHAF_BASMALLAH_DISPLAY_TEXT } from './mushaf-basmallah-display-text';
import { mushafCommonLigature } from './mushaf-common-ligature';
import { mushafSurahNameLigature } from './mushaf-surah-name-ligature';
import { MushafLineDto, PageMarkerDto } from '../../models/mushaf.models';

const SYNTHETIC_WORD = 'كلمة-تجريبية-١';

function buildLine(overrides: Partial<MushafLineDto> = {}): MushafLineDto {
  return {
    lineNumber: 1,
    lineType: 'ayah',
    isCentered: false,
    surahNumber: null,
    words: [
      {
        wordLocation: '99:1:1',
        verseKey: '99:1',
        wordNumber: 1,
        lineWordOrder: 1,
        textUthmani: SYNTHETIC_WORD,
        isAyahMarker: false,
      },
    ],
    ...overrides,
  };
}

describe('MushafLineComponent', () => {
  beforeEach(async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [MushafLineComponent],
    }).compileComponents();
  });

  it('distributes ayah-line words with space-between to fill the Mushaf page width', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine());
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.mushaf-line') as HTMLElement;
    expect(getComputedStyle(line).display).toBe('flex');
    expect(getComputedStyle(line).justifyContent).toBe('space-between');
    expect(getComputedStyle(line).width).toBe('100%');
  });

  it('keeps each stored Mushaf line on a single nowrap flex row', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine());
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.mushaf-line') as HTMLElement;
    expect(getComputedStyle(line).flexWrap).toBe('nowrap');
  });

  it('keeps surah-name lines centered', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput(
      'line',
      buildLine({ lineType: 'surah_name', isCentered: true, surahNumber: null, words: [] }),
    );
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.mushaf-line') as HTMLElement;
    expect(line.classList.contains('mushaf-line--surah-name')).toBe(true);
    expect(getComputedStyle(line).justifyContent).toBe('center');
    expect(fixture.nativeElement.querySelector('.mushaf-line__basmallah')).toBeNull();
    expect(fixture.nativeElement.querySelector('.mushaf-line__surah-title')).toBeNull();
  });

  it('renders surah-name lines with header box and name ligatures', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput(
      'line',
      buildLine({ lineType: 'surah_name', isCentered: true, surahNumber: 2, words: [] }),
    );
    fixture.componentRef.setInput('surahNameArabic', 'سورة-تجريبية-٢');
    fixture.detectChanges();

    const header = fixture.nativeElement.querySelector('.mushaf-line__surah-header') as HTMLElement;
    const nameGlyph = fixture.nativeElement.querySelector('.mushaf-line__surah-name-glyph') as HTMLElement;
    const srOnly = fixture.nativeElement.querySelector('.mushaf-line__sr-only') as HTMLElement;

    expect(header.textContent?.trim()).toBe(mushafCommonLigature('surah_header'));
    expect(nameGlyph.textContent?.trim()).toBe(mushafSurahNameLigature(2));
    expect(getComputedStyle(header).fontFamily).toContain('var(--qd-font-mushaf-common)');
    expect(getComputedStyle(nameGlyph).fontFamily).toContain('var(--qd-font-mushaf-surah-name-v2)');
    expect(srOnly.textContent?.trim()).toBe('سورة سورة-تجريبية-٢');
    expect(fixture.nativeElement.querySelector('qd-mushaf-word')).toBeNull();
  });

  it('renders basmallah lines with quran-common ligature and no word buttons', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput(
      'line',
      buildLine({ lineType: 'basmallah', isCentered: true, words: [] }),
    );
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.mushaf-line') as HTMLElement;
    const basmallah = fixture.nativeElement.querySelector('.mushaf-line__basmallah') as HTMLElement;

    expect(line.classList.contains('mushaf-line--basmallah')).toBe(true);
    expect(getComputedStyle(line).justifyContent).toBe('center');
    expect(fixture.componentInstance.basmallahText()).toBe(MUSHAF_BASMALLAH_DISPLAY_TEXT);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(MUSHAF_BASMALLAH_DISPLAY_TEXT);
    expect(getComputedStyle(basmallah).fontFamily).toContain('var(--qd-font-mushaf-common)');
    expect(getComputedStyle(basmallah).fontWeight).toBe('400');
    expect(fixture.nativeElement.querySelector('qd-mushaf-word')).toBeNull();
  });

  it('keeps explicitly centered lines centered', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine({ isCentered: true }));
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.mushaf-line') as HTMLElement;
    expect(line.classList.contains('mushaf-line--centered')).toBe(true);
    expect(getComputedStyle(line).justifyContent).toBe('center');
  });

  it('passes hoveredVerseKey through to its words', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine());
    fixture.componentRef.setInput('hoveredVerseKey', '99:1');
    fixture.detectChanges();

    const word = fixture.nativeElement.querySelector('button.mushaf-word') as HTMLButtonElement;
    expect(word.classList.contains('mushaf-word--hovered-ayah')).toBe(true);
  });

  it('re-emits ayahHover from its words', () => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine());
    fixture.detectChanges();

    const emitted: (string | null)[] = [];
    fixture.componentInstance.ayahHover.subscribe((verseKey) => emitted.push(verseKey));

    const word = fixture.nativeElement.querySelector('button.mushaf-word') as HTMLButtonElement;
    word.dispatchEvent(new Event('pointerenter'));
    word.dispatchEvent(new Event('pointerleave'));

    expect(emitted).toEqual(['99:1', null]);
  });

  it.each([
    { markerType: 'juz' as const, label: 'جزء 1' },
    { markerType: 'rub' as const, label: 'ربع 2' },
    { markerType: 'hizb' as const, label: 'حزب 1' },
    { markerType: 'sajda' as const, label: 'سجدة 3' },
  ])('does not render hidden $markerType page markers', ({ markerType, label }) => {
    const fixture = TestBed.createComponent(MushafLineComponent);
    fixture.componentRef.setInput('line', buildLine({ lineNumber: 3 }));
    fixture.componentRef.setInput('markers', [
      {
        markerType,
        markerNumber: markerType === 'rub' ? 2 : markerType === 'hizb' ? 1 : markerType === 'juz' ? 1 : 3,
        verseKey: '99:1',
        lineNumber: 3,
        wordLocation: '99:1:1',
        sajdahType: markerType === 'sajda' ? 'test-type' : null,
      } satisfies PageMarkerDto,
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('qd-mushaf-marker')).toBeNull();
    expect(root.textContent ?? '').not.toContain(label);
  });
});
