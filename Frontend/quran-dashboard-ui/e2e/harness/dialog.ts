import { expect, type Locator, type Page } from '@playwright/test';

// Critical dialogs (destructive confirmations, restore) must be dismissable by keyboard and must
// return focus to the trigger that opened them, so the interaction is safe for keyboard-only operators.
export async function expectEscapeCloses(page: Page, dialog: Locator): Promise<void> {
  await expect(dialog).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
}

export async function expectFocusReturnsToTrigger(
  page: Page,
  trigger: Locator,
  closeDialog: () => Promise<void>,
): Promise<void> {
  const triggerTestId = await trigger.getAttribute('data-testid');
  await closeDialog();
  const focused = await page.evaluate(() => document.activeElement?.getAttribute('data-testid') ?? null);
  expect(focused).toBe(triggerTestId);
}
