import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SegmentDataRowsComponent } from './segment-data-rows.component';
import { RenderedSegmentViewModel } from '../../models/mushaf.models';

const segments: RenderedSegmentViewModel[] = [
  {
    segmentLocation: '2:25:1:1',
    segmentNumber: 1,
    segmentColorSlot: 1,
    color: 'rgb(10, 20, 30)',
    segmentKind: 'prefix',
    segmentDisplayText: 'وَ',
    isMissing: false,
    segmentPos: 'CONJ',
    segmentPosLabel: { ar: 'حرف', en: 'Particle' },
    segmentI3rabArabic: 'حرف عطف',
    i3rabStatus: 'complete',
  },
  {
    segmentLocation: '2:25:1:2',
    segmentNumber: 2,
    segmentColorSlot: 2,
    color: 'rgb(40, 50, 60)',
    segmentKind: 'stem',
    segmentDisplayText: 'بَشِّرِ',
    isMissing: false,
    segmentPos: 'V',
    segmentPosLabel: { ar: 'فعل', en: 'Verb' },
    segmentI3rabArabic: 'فعل أمر',
    i3rabStatus: 'complete',
  },
];

function rows(fixture: ComponentFixture<SegmentDataRowsComponent>): HTMLElement[] {
  return Array.from(
    (fixture.nativeElement as HTMLElement).querySelectorAll('.segment-data-rows__row'),
  );
}

describe('SegmentDataRowsComponent — D37 non-interactive morphology rows', () => {
  let fixture: ComponentFixture<SegmentDataRowsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SegmentDataRowsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SegmentDataRowsComponent);
    fixture.componentRef.setInput('segments', segments);
    fixture.detectChanges();
  });

  it('renders one row per segment', () => {
    expect(rows(fixture)).toHaveLength(2);
  });

  it('gives no row a button, anchor or other control role', () => {
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('button')).toBeNull();
    expect(root.querySelector('a')).toBeNull();
    for (const row of rows(fixture)) {
      expect(row.tagName).toBe('DIV');
      expect(row.getAttribute('role')).toBeNull();
      expect(row.hasAttribute('tabindex')).toBe(false);
      expect(row.classList.contains('qd-interactive-surface')).toBe(false);
    }
  });

  it('keeps the pointer affordance off every row', () => {
    for (const row of rows(fixture)) {
      expect(getComputedStyle(row).cursor).toBe('default');
    }
  });

  it('preserves the morphology colour, number, part-of-speech and إعراب of each segment', () => {
    const root = fixture.nativeElement as HTMLElement;
    const [first] = rows(fixture);

    expect(first.style.getPropertyValue('--segment-accent')).toBe('rgb(10, 20, 30)');
    expect(first.getAttribute('data-segment-slot')).toBe('1');
    expect(first.textContent).toContain('وَ');
    expect(first.textContent).toContain('حرف');
    expect(
      root.querySelectorAll('[data-testid="segment-i3rab-label"]')[0]?.textContent?.trim(),
    ).toBe('حرف عطف');
  });

  it('renders the missing-segment placeholder as text, never as an action', () => {
    fixture.componentRef.setInput('segments', [{ ...segments[0], isMissing: true }]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const placeholder = root.querySelector('[data-testid="segment-row-placeholder"]');

    expect(placeholder?.textContent?.trim()).toBe('…');
    expect(root.querySelector('button')).toBeNull();
  });
});
