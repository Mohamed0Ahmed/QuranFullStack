import { RenderedSegmentViewModel } from '../models/mushaf.models';

export interface UthmaniSegmentSlice {
  text: string;
  color: string;
  segmentColorSlot: number;
  isMissing: boolean;
  /** UTF-16 offset in fullWordText; -1 when the slice is a missing placeholder. */
  rangeStart: number;
  /** UTF-16 offset in fullWordText (exclusive); -1 when the slice is a missing placeholder. */
  rangeEnd: number;
}

const MISSING_SLICE_TEXT = '…';
const graphemeSegmenter = new Intl.Segmenter('ar', { granularity: 'grapheme' });

function splitGraphemes(text: string): Intl.SegmentData[] {
  return [...graphemeSegmenter.segment(text)];
}

export function countGraphemes(text: string): number {
  return splitGraphemes(text).length;
}

function sliceTextByGraphemeRange(
  text: string,
  startGrapheme: number,
  endGrapheme: number,
): Pick<UthmaniSegmentSlice, 'text' | 'rangeStart' | 'rangeEnd'> {
  const segments = splitGraphemes(text);

  if (segments.length === 0 || startGrapheme >= endGrapheme) {
    return { text: '', rangeStart: 0, rangeEnd: 0 };
  }

  const clampedStart = Math.max(0, Math.min(startGrapheme, segments.length));
  const clampedEnd = Math.max(clampedStart, Math.min(endGrapheme, segments.length));
  const rangeStart = segments[clampedStart].index;
  const endSegment = segments[clampedEnd - 1];
  const rangeEnd = endSegment.index + endSegment.segment.length;

  return {
    text: text.slice(rangeStart, rangeEnd),
    rangeStart,
    rangeEnd,
  };
}

function lastAvailableSegmentIndex(segments: RenderedSegmentViewModel[]): number {
  for (let index = segments.length - 1; index >= 0; index--) {
    const segment = segments[index];
    if (!segment.isMissing && segment.segmentDisplayText) {
      return index;
    }
  }

  return -1;
}

function missingSlice(segment: RenderedSegmentViewModel): UthmaniSegmentSlice {
  return {
    text: MISSING_SLICE_TEXT,
    color: segment.color,
    segmentColorSlot: segment.segmentColorSlot,
    isMissing: true,
    rangeStart: -1,
    rangeEnd: -1,
  };
}

export function buildUthmaniSegmentSlices(
  fullWordText: string,
  segments: RenderedSegmentViewModel[],
): UthmaniSegmentSlice[] {
  const slices: UthmaniSegmentSlice[] = [];
  let cursorGrapheme = 0;
  const totalGraphemes = countGraphemes(fullWordText);
  const lastAvailableIndex = lastAvailableSegmentIndex(segments);

  for (let index = 0; index < segments.length; index++) {
    const segment = segments[index];

    if (segment.isMissing) {
      slices.push(missingSlice(segment));
      continue;
    }

    if (cursorGrapheme >= totalGraphemes || !segment.segmentDisplayText) {
      slices.push(missingSlice(segment));
      continue;
    }

    const formGraphemeCount = countGraphemes(segment.segmentDisplayText.trim());
    const isLastAvailable = index === lastAvailableIndex;
    const endGrapheme = isLastAvailable ? totalGraphemes : cursorGrapheme + formGraphemeCount;
    const slice = sliceTextByGraphemeRange(fullWordText, cursorGrapheme, endGrapheme);

    slices.push({
      text: slice.text.length > 0 ? slice.text : MISSING_SLICE_TEXT,
      color: segment.color,
      segmentColorSlot: segment.segmentColorSlot,
      isMissing: slice.text.length === 0,
      rangeStart: slice.text.length > 0 ? slice.rangeStart : -1,
      rangeEnd: slice.text.length > 0 ? slice.rangeEnd : -1,
    });

    cursorGrapheme = endGrapheme;
  }

  return slices;
}
