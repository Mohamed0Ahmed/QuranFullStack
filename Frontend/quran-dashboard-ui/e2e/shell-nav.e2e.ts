import { expect, test } from './fixtures/app-test';

test('the navbar links reach the Mushaf reader', async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-link--mushaf').click();

  await expect(page).toHaveURL(/\/dashboard\/mushaf/);
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
});

test('the words dropdown reaches the words hub', async ({ page }) => {
  await page.goto('/dashboard');

  // Hover, not click: the words item opens on `mouseenter` and the button's own click handler
  // toggles it shut again, so a Playwright click would open and close the menu in one action.
  await page.getByTestId('nav-words-trigger').hover();
  await page.locator('#words-menu').getByRole('link', { name: 'الرئيسية' }).click();

  await expect(page).toHaveURL(/\/dashboard\/words$/);
  await expect(page.getByTestId('words-hub-title')).toBeVisible();
});

test('the more dropdown reaches a placeholder section', async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-more-trigger').click();
  await page.getByTestId('nav-menu-link--mutashabihat').click();

  await expect(page).toHaveURL(/\/mutashabihat$/);
  await expect(page.getByRole('heading', { name: 'المتشابهات', level: 1 })).toBeVisible();
});
