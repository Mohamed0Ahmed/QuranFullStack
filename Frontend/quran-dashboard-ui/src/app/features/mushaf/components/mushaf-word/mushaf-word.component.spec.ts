import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { MushafWordComponent } from './mushaf-word.component';
import { MushafWordDto } from '../../models/mushaf.models';

const SYNTHETIC_WORD = 'كلمة-تجريبية-١';
const MARKER_TEXT_PLACEHOLDER = 'علامة-تجريبية';
const SYNTHETIC_AYAH_DIGIT = '٧';
const WAQF_THREE_DOTS = '\u06DB';

function buildWord(overrides: Partial<MushafWordDto> = {}): MushafWordDto {
  return {
    wordLocation: '99:1:1',
    verseKey: '99:1',
    wordNumber: 1,
    lineWordOrder: 1,
    textUthmani: SYNTHETIC_WORD,
    isAyahMarker: false,
    ...overrides,
  };
}

describe('MushafWordComponent', () => {
  it('renders a waqf mark without a trailing space before it', () => {
    const raw = `${SYNTHETIC_WORD} ${WAQF_THREE_DOTS}`;
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ textUthmani: raw }));
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    const rendered = button.textContent?.trim() ?? '';
    expect(rendered).toBe(`${SYNTHETIC_WORD}${WAQF_THREE_DOTS}`);
    expect(rendered).toContain(WAQF_THREE_DOTS);
    expect(rendered).not.toMatch(/\s\u06DB$/);
  });

  it('does not mutate the raw textUthmani model value', () => {
    const raw = `${SYNTHETIC_WORD} ${WAQF_THREE_DOTS}`;
    const word = buildWord({ textUthmani: raw });
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', word);
    fixture.detectChanges();

    expect(fixture.componentInstance.word().textUthmani).toBe(raw);
  });

  it('does not apply fixed inter-word margin on the word button', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord());
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(['0px', '0']).toContain(getComputedStyle(button).marginInlineEnd);
  });

  it('emits wordSelect when a clickable word is clicked', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3' }));
    fixture.detectChanges();

    const wordSelect = vi.fn();
    fixture.componentInstance.wordSelect.subscribe(wordSelect);

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();

    expect(wordSelect).toHaveBeenCalledWith('99:1:3');
  });

  it('applies selected-word styling when selectedWordLocation matches', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3' }));
    fixture.componentRef.setInput('selectedWordLocation', '99:1:3');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--selected-word')).toBe(true);
    expect(button.classList.contains('mushaf-word--selected')).toBe(false);
  });

  it('does not apply ayah background styling on the word button', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3', verseKey: '99:1' }));
    fixture.componentRef.setInput('selectedWordLocation', '99:1:5');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--selected')).toBe(false);
    expect(button.classList.contains('mushaf-word--selected-word')).toBe(false);
  });

  it('uses Amiri token for regular Mushaf words', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord());
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(getComputedStyle(button).fontFamily).toBe('var(--qd-font-quran)');
  });

  it('uses Uthmanic Hafs token for ayah-end marker digits', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput(
      'word',
      buildWord({
        wordLocation: '99:1:9',
        textUthmani: SYNTHETIC_AYAH_DIGIT,
        isAyahMarker: true,
      }),
    );
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--marker')).toBe(true);
    expect(getComputedStyle(button).fontFamily).toBe('var(--qd-font-quran-ayah-marker)');
    expect(getComputedStyle(button).fontSize).toBe('var(--qd-mushaf-ayah-marker-font-size)');
    expect(getComputedStyle(button).color).toBe('var(--qd-mushaf-ayah-marker-color)');
    expect(fixture.componentInstance.word().textUthmani).toBe(SYNTHETIC_AYAH_DIGIT);
  });

  it('applies the hovered-ayah wash to every word whose verseKey matches', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3', verseKey: '99:1' }));
    fixture.componentRef.setInput('hoveredVerseKey', '99:1');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--hovered-ayah')).toBe(true);
  });

  it('does not apply the hovered-ayah wash to a word of another ayah', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:2:1', verseKey: '99:2' }));
    fixture.componentRef.setInput('hoveredVerseKey', '99:1');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--hovered-ayah')).toBe(false);
  });

  it('excludes the ayah-marker glyph from the hovered-ayah wash', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput(
      'word',
      buildWord({
        wordLocation: '99:1:9',
        verseKey: '99:1',
        textUthmani: SYNTHETIC_AYAH_DIGIT,
        isAyahMarker: true,
      }),
    );
    fixture.componentRef.setInput('hoveredVerseKey', '99:1');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--hovered-ayah')).toBe(false);
  });

  it('keeps both classes when the hovered ayah contains the selected word, letting selection win', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3', verseKey: '99:1' }));
    fixture.componentRef.setInput('hoveredVerseKey', '99:1');
    fixture.componentRef.setInput('selectedWordLocation', '99:1:3');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.classList.contains('mushaf-word--hovered-ayah')).toBe(true);
    expect(button.classList.contains('mushaf-word--selected-word')).toBe(true);
  });

  it.each([
    { event: 'pointerenter', expected: '99:1' },
    { event: 'focus', expected: '99:1' },
    { event: 'pointerleave', expected: null },
    { event: 'blur', expected: null },
  ])('emits $expected on $event of a clickable word', ({ event, expected }) => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', buildWord({ wordLocation: '99:1:3', verseKey: '99:1' }));
    fixture.detectChanges();

    const ayahHover = vi.fn();
    fixture.componentInstance.ayahHover.subscribe(ayahHover);

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.dispatchEvent(new Event(event));

    expect(ayahHover).toHaveBeenCalledWith(expected);
  });

  it('does not emit ayahHover for ayah-end markers', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput(
      'word',
      buildWord({
        wordLocation: '99:1:9',
        verseKey: '99:1',
        textUthmani: SYNTHETIC_AYAH_DIGIT,
        isAyahMarker: true,
      }),
    );
    fixture.detectChanges();

    const ayahHover = vi.fn();
    fixture.componentInstance.ayahHover.subscribe(ayahHover);

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.dispatchEvent(new Event('pointerenter'));

    expect(ayahHover).not.toHaveBeenCalled();
  });

  it('does not emit wordSelect for ayah-end markers', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput(
      'word',
      buildWord({
        wordLocation: '2:25:5',
        textUthmani: MARKER_TEXT_PLACEHOLDER,
        isAyahMarker: true,
      }),
    );
    fixture.detectChanges();

    const wordSelect = vi.fn();
    fixture.componentInstance.wordSelect.subscribe(wordSelect);

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    button.click();
    expect(wordSelect).not.toHaveBeenCalled();
  });
});
