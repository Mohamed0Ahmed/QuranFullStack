import type { Page } from '@playwright/test';

import { expect } from './app-test';

export async function openReader(page: Page): Promise<void> {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
}

export async function selectFirstWord(page: Page): Promise<void> {
  // Ayah markers render as `[disabled]` buttons (mushaf-word.component.html), so clicking one
  // silently does nothing — the data-is-marker filter keeps the click on a selectable word.
  await page.locator('[data-word-location][data-is-marker="false"]').first().click();
}

export function ayahTab(page: Page, name: string) {
  return page.getByRole('tab', { name, exact: true });
}
