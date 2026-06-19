export const SOURCE_SELECTOR_PANEL_MAX_HEIGHT_VAR = '--source-selector-panel-max-height';

/** Minimum panel height (~12rem at a 16px root font size). */
export const SOURCE_SELECTOR_PANEL_MIN_HEIGHT_PX = 12 * 16;

export const SOURCE_SELECTOR_PANEL_VIEWPORT_PADDING_PX = 12;

export function sourceSelectorPanelMaxHeightPx(viewportHeight: number, panelTop: number): number {
  const available = viewportHeight - panelTop - SOURCE_SELECTOR_PANEL_VIEWPORT_PADDING_PX;
  return Math.max(SOURCE_SELECTOR_PANEL_MIN_HEIGHT_PX, available);
}
