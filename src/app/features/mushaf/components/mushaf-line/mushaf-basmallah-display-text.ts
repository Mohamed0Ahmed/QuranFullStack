import { MUSHAF_BASMALLAH_LIGATURE } from './mushaf-common-ligature';

/**
 * Presentation-only basmallah line glyph.
 *
 * `basmallah` lines arrive from the API with `words: []`; the printed form is
 * normally drawn from the page Mushaf font (QPC). Render the `bismillah` ligature
 * from `quran-common.woff2` via `mushaf-common.ligatures.json`. Does not alter
 * API payloads or stored `textUthmani` on word rows.
 */
export { MUSHAF_BASMALLAH_LIGATURE as MUSHAF_BASMALLAH_DISPLAY_TEXT };
