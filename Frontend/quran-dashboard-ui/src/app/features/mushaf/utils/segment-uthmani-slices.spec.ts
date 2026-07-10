import { describe, expect, it } from 'vitest';

import { RenderedSegmentViewModel } from '../models/mushaf.models';
import { buildUthmaniSegmentSlices, countGraphemes } from './segment-uthmani-slices';

function buildSegment(
  partial: Pick<RenderedSegmentViewModel, 'segmentNumber' | 'segmentColorSlot' | 'color'> &
    Partial<RenderedSegmentViewModel>,
): RenderedSegmentViewModel {
  return {
    segmentLocation: `2:25:3:${partial.segmentNumber}`,
    segmentKind: 'STEM',
    segmentDisplayText: null,
    isMissing: false,
    segmentPos: null,
    segmentPosLabel: null,
    segmentI3rabArabic: null,
    i3rabStatus: null,
    ...partial,
  };
}

describe('buildUthmaniSegmentSlices', () => {
  it('slices the authoritative Uthmani text contiguously with no inserted spaces', () => {
    const fullWordText = 'الرَّحِيمِ';
    const segments = [
      buildSegment({
        segmentNumber: 1,
        segmentColorSlot: 1,
        color: 'slot-1',
        segmentDisplayText: 'ٱل',
      }),
      buildSegment({
        segmentNumber: 2,
        segmentColorSlot: 2,
        color: 'slot-2',
        segmentDisplayText: 'رَّحِيمِ',
      }),
    ];

    const slices = buildUthmaniSegmentSlices(fullWordText, segments);

    expect(slices.map((slice) => slice.text).join('')).toBe(fullWordText);
    expect(slices[0]).toMatchObject({
      text: 'ال',
      color: 'slot-1',
      isMissing: false,
      rangeStart: 0,
    });
    expect(slices[1]).toMatchObject({
      text: 'رَّحِيمِ',
      color: 'slot-2',
      isMissing: false,
    });
    expect(slices[1].rangeEnd).toBe(fullWordText.length);
  });

  it('uses grapheme counts so combining marks stay inside their segment slice', () => {
    const fullWordText = 'كلمة-مركبة';
    const segments = [
      buildSegment({
        segmentNumber: 1,
        segmentColorSlot: 1,
        color: 'slot-1',
        segmentDisplayText: 'كل',
      }),
      buildSegment({
        segmentNumber: 2,
        segmentColorSlot: 2,
        color: 'slot-2',
        segmentDisplayText: 'مة',
      }),
    ];

    const slices = buildUthmaniSegmentSlices(fullWordText, segments);

    expect(countGraphemes(slices[0].text)).toBe(2);
    expect(countGraphemes(slices[1].text)).toBe(countGraphemes(fullWordText) - 2);
    expect(slices.map((slice) => slice.text).join('')).toBe(fullWordText);
  });

  it('lets the last available segment absorb the remaining Uthmani text when form lengths differ', () => {
    const fullWordText = 'كلمة-كاملة';
    const segments = [
      buildSegment({
        segmentNumber: 1,
        segmentColorSlot: 1,
        color: 'slot-1',
        segmentDisplayText: 'قطعة',
      }),
      buildSegment({
        segmentNumber: 2,
        segmentColorSlot: 2,
        color: 'slot-2',
        segmentDisplayText: 'باقي',
      }),
    ];

    const slices = buildUthmaniSegmentSlices(fullWordText, segments);

    expect(slices.map((slice) => slice.text).join('')).toBe(fullWordText);
    expect(slices[1].text).toBe('-كاملة');
  });

  it('uses a placeholder slice for missing segments without advancing the Uthmani cursor', () => {
    const fullWordText = 'كلمة-تجريبية';
    const segments = [
      buildSegment({
        segmentNumber: 1,
        segmentColorSlot: 1,
        color: 'slot-1',
        segmentDisplayText: 'كلمة',
      }),
      buildSegment({
        segmentNumber: 2,
        segmentColorSlot: 2,
        color: 'slot-2',
        isMissing: true,
        segmentDisplayText: null,
      }),
    ];

    const slices = buildUthmaniSegmentSlices(fullWordText, segments);

    expect(slices[0].text).toBe(fullWordText);
    expect(slices[1]).toMatchObject({
      text: '…',
      isMissing: true,
      rangeStart: -1,
      rangeEnd: -1,
    });
  });
});
