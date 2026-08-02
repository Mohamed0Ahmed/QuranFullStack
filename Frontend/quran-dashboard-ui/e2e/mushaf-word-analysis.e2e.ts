import { expect, test } from './fixtures/app-test';
import { openReader, selectFirstWord } from './fixtures/mushaf';

test('selecting a word renders its morphology summary and identity links', async ({ page }) => {
  await openReader(page);

  await selectFirstWord(page);

  await expect(page.getByTestId('selected-word-section')).toBeVisible();
  await expect(page).toHaveURL(/[?&]word=/);
  await expect(page.getByTestId('word-morphology-summary')).toBeVisible();
  await expect(page.getByTestId('word-identity-summary')).toBeVisible();
});
