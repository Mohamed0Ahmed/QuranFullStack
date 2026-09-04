import { expect, test } from './fixtures/app-test';

const CANONICAL_READ = {
  annotation: [
    { type: 'canonical-read' },
    { type: 'fixture-policy', description: 'canonical-read-only' },
  ],
};

test('the navbar links reach the Mushaf reader', CANONICAL_READ, async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-link--mushaf').click();

  await expect(page).toHaveURL(/\/dashboard\/mushaf/);
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
});

test('the words dropdown reaches the words hub', CANONICAL_READ, async ({ page }) => {
  await page.goto('/dashboard');

  // Hover is the pointer path this spec pins: the item opens on hover-intent alone, and the
  // link click below proves the menu works without ever clicking the trigger. (A trigger click
  // after hover now also works — the hover/click fight was fixed — but that path is pinned by
  // the unit spec, not here.)
  await page.getByTestId('nav-words-trigger').hover();
  await page.locator('#words-menu').getByRole('link', { name: 'الرئيسية' }).click();

  await expect(page).toHaveURL(/\/dashboard\/words$/);
  await expect(page.getByTestId('words-hub-title')).toBeVisible();
});

test('the more dropdown reaches a placeholder section', CANONICAL_READ, async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-more-trigger').click();
  await page.getByTestId('nav-menu-link--mutashabihat').click();

  await expect(page).toHaveURL(/\/mutashabihat$/);
  await expect(page.getByRole('heading', { name: 'المتشابهات', level: 1 })).toBeVisible();
});
