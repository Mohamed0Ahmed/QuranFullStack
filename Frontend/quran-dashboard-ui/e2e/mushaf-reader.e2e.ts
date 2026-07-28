import { expect, test } from './fixtures/app-test';
import { openReader } from './fixtures/mushaf';

test('the reader opens on page 1 and renders the Mushaf page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');

  await expect(page.getByTestId('mushaf-reader-page')).toHaveAttribute('dir', 'rtl');
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('next then previous returns to the starting page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('mushaf-next-page').click();

  await expect(page).toHaveURL(/[?&]page=2(&|$)/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');

  await page.getByTestId('mushaf-prev-page').click();

  // In-app paging writes the page key unconditionally (mushaf-reader.facade.ts changePage);
  // only session restoration omits the default. A redundant page=1 still hydrates page 1.
  await expect(page).toHaveURL(/[?&]page=1(&|$)/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('a page deep link hydrates the reader', async ({ page }) => {
  await page.goto('/dashboard/mushaf?page=5');

  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('5');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page).toHaveURL(/[?&]page=5(&|$)/);
});

test('the surah jump picker moves the reader to another page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('surah-jump-picker-trigger').click();
  await expect(page.getByTestId('surah-jump-picker-panel')).toBeVisible();

  await page.getByTestId('surah-jump-picker-search').fill('البقرة');
  await page.getByTestId('surah-jump-picker-row').first().click();

  await expect(page.getByTestId('surah-jump-picker-panel')).toHaveCount(0);
  // Al-Baqara opens page 2 of the 604-page Madani mushaf — a layout invariant, not seed data.
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');
  await expect(page).toHaveURL(/[?&]page=2(&|$)/);
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
});

test('the Mushaf renders Uthmani chrome glyphs and Amiri Quran text', async ({ page }) => {
  await openReader(page);

  await expect(page.getByTestId('mushaf-page-surah-glyph').first()).toBeVisible();
  await expect(page.getByTestId('mushaf-page-juz-glyph').first()).toBeVisible();

  const words = page.locator('[data-word-location]');
  await expect(words.first()).toBeVisible();

  // The reader must render in Amiri, never UthmanicHafs_V22, which mis-renders U+06DF
  // (Frontend/quran-dashboard-ui/README.md).
  const fontFamily = await words
    .first()
    .evaluate((element) => getComputedStyle(element).fontFamily);
  expect(fontFamily).toContain('Amiri');
});
