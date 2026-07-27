import { expect, test } from './fixtures/app-test';

test('dashboard home renders the Arabic welcome heading and the surface cards', async ({ page }) => {
  await page.goto('/dashboard');

  await expect(
    page.getByRole('heading', { name: 'مرحباً بك في المنهج القرآني', level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole('link', { name: 'المصحف والآيات' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'الكلمات والجذور' })).toBeVisible();
});

test('the document is Arabic, RTL and themed before paint', async ({ page }) => {
  await page.goto('/dashboard');

  const html = page.locator('html');
  await expect(html).toHaveAttribute('lang', 'ar');
  await expect(html).toHaveAttribute('dir', 'rtl');
  await expect(html).toHaveAttribute('data-theme', /^(light|dark)$/);
});

test('the root path redirects to the dashboard', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL(/\/dashboard$/);
});
