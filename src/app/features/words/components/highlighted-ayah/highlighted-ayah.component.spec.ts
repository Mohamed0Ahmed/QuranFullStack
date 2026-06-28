import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { HighlightedAyahComponent } from './highlighted-ayah.component';
import { AyahWordForHighlightDto } from '../../models/unique-words.models';

const words: AyahWordForHighlightDto[] = [
  { quranWordId: 10, textUthmani: 'ألف', isAyahMarker: false },
  { quranWordId: 11, textUthmani: 'باء', isAyahMarker: false },
  { quranWordId: 12, textUthmani: 'جيم', isAyahMarker: false },
  { quranWordId: 13, textUthmani: '٣', isAyahMarker: true },
];

describe('HighlightedAyahComponent', () => {
  it('highlights only matched quran word ids', async () => {
    await TestBed.configureTestingModule({
      imports: [HighlightedAyahComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(HighlightedAyahComponent);
    fixture.componentRef.setInput('words', words);
    fixture.componentRef.setInput('matchedQuranWordIds', [11]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const tokens = root.querySelectorAll('.highlighted-ayah__word');
    expect(tokens).toHaveLength(3);
    expect(tokens[0].classList.contains('highlighted-ayah__word--matched')).toBe(false);
    expect(tokens[1].classList.contains('highlighted-ayah__word--matched')).toBe(true);
    expect(tokens[2].classList.contains('highlighted-ayah__word--matched')).toBe(false);
  });

  it('omits ayah marker words', async () => {
    await TestBed.configureTestingModule({
      imports: [HighlightedAyahComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(HighlightedAyahComponent);
    fixture.componentRef.setInput('words', words);
    fixture.componentRef.setInput('matchedQuranWordIds', [11]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).not.toContain('٣');
  });
});
