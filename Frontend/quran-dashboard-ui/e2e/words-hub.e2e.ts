import { expect, test } from './fixtures/app-test';

// One card per WORDS_EXPLAINER_ORDER key (words/models/words-explainer.content.ts).
const HUB_CARD_COUNT = 5;

test('the words hub renders its title and the five explorer cards', async ({ page }) => {
  await page.goto('/dashboard/words');

  await expect(page.getByTestId('words-hub-title')).toBeVisible();
  await expect(page.getByTestId('words-hub-subtitle')).toBeVisible();

  const cards = page.getByTestId(/^words-hub-card--/);
  await expect(cards.first()).toBeVisible();
  await expect(cards).toHaveCount(HUB_CARD_COUNT);
});

test('a hub card opens its explorer', async ({ page }) => {
  await page.goto('/dashboard/words');

  await page.getByTestId('words-hub-card--roots').click();

  await expect(page).toHaveURL(/\/dashboard\/words\/roots(\?|$)/);
  await expect(page.getByTestId('roots-explorer-page-title')).toBeVisible();
});
