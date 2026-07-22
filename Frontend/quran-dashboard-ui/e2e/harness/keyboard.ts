import { expect, type Page } from '@playwright/test';

export async function pressTab(page: Page, times = 1): Promise<void> {
  for (let index = 0; index < times; index += 1) {
    await page.keyboard.press('Tab');
  }
}

export function activeElementTestId(page: Page): Promise<string | null> {
  return page.evaluate(() => document.activeElement?.getAttribute('data-testid') ?? null);
}

// Keyboard-driven interactions (open/close, navigate) must return focus where the user left it, so a
// keyboard-only operator never loses their place.
export async function expectFocusRestored(page: Page, interaction: () => Promise<void>): Promise<void> {
  const before = await activeElementTestId(page);
  await interaction();
  expect(await activeElementTestId(page)).toBe(before);
}
