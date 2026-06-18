/**
 * Presentation-only Mushaf word display shaping.
 *
 * Stored `textUthmani` may end with `SPACE + Quranic combining mark` (e.g. waqf
 * signs in U+06D6–U+06ED). Per-word inline buttons with preserved whitespace
 * isolate that mark from its base letter, so the engine draws it on a dotted-
 * circle placeholder. This helper removes only the trailing gap before the mark
 * for rendering; the mark stays visible and attached to the word body.
 */
const TRAILING_SPACE_BEFORE_QURANIC_MARK = /\s+([\u06D6-\u06ED])$/u;

export function toMushafWordDisplayText(textUthmani: string): string {
  return textUthmani.replace(TRAILING_SPACE_BEFORE_QURANIC_MARK, '$1');
}
