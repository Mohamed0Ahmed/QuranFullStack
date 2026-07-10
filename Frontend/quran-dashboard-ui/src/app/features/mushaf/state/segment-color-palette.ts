
export const SEGMENT_COLOR_PALETTE = [
  'var(--qd-segment-cat-1)',
  'var(--qd-segment-cat-2)',
  'var(--qd-segment-cat-3)',
  'var(--qd-segment-cat-4)',
  'var(--qd-segment-cat-5)',
  'var(--qd-segment-cat-6)',
] as const;

export function segmentSlotToColor(slot: number): string {
  const index = (slot - 1) % SEGMENT_COLOR_PALETTE.length;
  return SEGMENT_COLOR_PALETTE[index >= 0 ? index : 0];
}
