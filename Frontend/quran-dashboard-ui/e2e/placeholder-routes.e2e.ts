import { expect, test } from './fixtures/app-test';

test('an unbuilt section renders its placeholder sentence', async ({ page }) => {
  await page.goto('/mutashabihat');

  await expect(page.getByRole('heading', { name: 'المتشابهات', level: 1 })).toBeVisible();
  await expect(page.getByText('سيتم ربط هذا القسم ضمن خطة الميزات التالية.')).toBeVisible();
});

test('an unknown route falls back to the dashboard', async ({ page }) => {
  await page.goto('/definitely-not-a-route');

  await expect(page).toHaveURL(/\/dashboard$/);
});
