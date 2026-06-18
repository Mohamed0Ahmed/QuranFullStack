import { UthmaniSegmentSlice } from './segment-uthmani-slices';

export const SEGMENT_HIGHLIGHT_SLOT_COUNT = 6;

export function segmentHighlightName(slot: number): string {
  return `qd-segment-slot-${slot}`;
}

export function supportsCssCustomHighlights(): boolean {
  return typeof CSS !== 'undefined' && 'highlights' in CSS;
}

export function clearSegmentHighlights(slots: Iterable<number> = rangeSlots()): void {
  if (!supportsCssCustomHighlights()) {
    return;
  }

  for (const slot of slots) {
    CSS.highlights.delete(segmentHighlightName(slot));
  }
}

function rangeSlots(): number[] {
  return Array.from({ length: SEGMENT_HIGHLIGHT_SLOT_COUNT }, (_, index) => index + 1);
}

function getHostTextNode(host: HTMLElement): Text | null {
  for (const child of host.childNodes) {
    if (child.nodeType === Node.TEXT_NODE) {
      return child as Text;
    }
  }

  return null;
}

export function applySegmentWordHighlights(
  host: HTMLElement,
  fullWordText: string,
  slices: UthmaniSegmentSlice[],
): number[] {
  clearSegmentHighlights();

  if (!supportsCssCustomHighlights()) {
    return [];
  }

  const textNode = getHostTextNode(host);
  if (!textNode || textNode.textContent !== fullWordText) {
    return [];
  }

  const rangesBySlot = new Map<number, Range[]>();

  for (const slice of slices) {
    if (slice.isMissing || slice.rangeStart < 0 || slice.rangeEnd <= slice.rangeStart) {
      continue;
    }

    const range = document.createRange();
    range.setStart(textNode, slice.rangeStart);
    range.setEnd(textNode, slice.rangeEnd);

    const existing = rangesBySlot.get(slice.segmentColorSlot) ?? [];
    existing.push(range);
    rangesBySlot.set(slice.segmentColorSlot, existing);
  }

  const usedSlots: number[] = [];

  for (const [slot, ranges] of rangesBySlot) {
    CSS.highlights.set(segmentHighlightName(slot), new Highlight(...ranges));
    usedSlots.push(slot);
  }

  return usedSlots;
}
