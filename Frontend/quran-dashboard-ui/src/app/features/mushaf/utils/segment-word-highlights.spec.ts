import { afterEach, describe, expect, it } from 'vitest';

import { buildUthmaniSegmentSlices } from './segment-uthmani-slices';
import {
  applySegmentWordHighlights,
  clearSegmentHighlights,
  segmentHighlightName,
  supportsCssCustomHighlights,
} from './segment-word-highlights';

describe('segment-word-highlights', () => {
  afterEach(() => {
    clearSegmentHighlights();
  });

  it('reports whether CSS Custom Highlights are available in the current runtime', () => {
    expect(typeof supportsCssCustomHighlights()).toBe('boolean');
  });

  it('registers highlight ranges per color slot when the API is available', () => {
    if (!supportsCssCustomHighlights()) {
      return;
    }

    const host = document.createElement('span');
    const fullWordText = 'كلمة-تجريب';
    host.textContent = fullWordText;
    document.body.appendChild(host);

    const slices = buildUthmaniSegmentSlices(fullWordText, [
      {
        segmentLocation: '1:1:1:1',
        segmentNumber: 1,
        segmentKind: 'PREFIX',
        segmentDisplayText: 'كل',
        isMissing: false,
        segmentColorSlot: 1,
        color: '#3d6b8e',
        segmentPos: null,
        segmentPosLabel: null,
        segmentI3rabArabic: null,
        i3rabStatus: null,
      },
      {
        segmentLocation: '1:1:1:2',
        segmentNumber: 2,
        segmentKind: 'STEM',
        segmentDisplayText: 'مة',
        isMissing: false,
        segmentColorSlot: 2,
        color: '#9a6b3c',
        segmentPos: null,
        segmentPosLabel: null,
        segmentI3rabArabic: null,
        i3rabStatus: null,
      },
    ]);

    const usedSlots = applySegmentWordHighlights(host, fullWordText, slices);

    expect(usedSlots).toEqual([1, 2]);
    expect(CSS.highlights.get(segmentHighlightName(1))?.size).toBe(1);
    expect(CSS.highlights.get(segmentHighlightName(2))?.size).toBe(1);

    host.remove();
  });
});
