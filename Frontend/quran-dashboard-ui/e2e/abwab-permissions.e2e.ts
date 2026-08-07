import { expect, test } from './fixtures/app-test';

const API_BASE = 'https://localhost:5015';

test('anonymous visitors retain Abwab reads while write controls and restored write overlays stay unavailable', async ({ page }) => {
  await page.goto('/abwab?view=tree&q=%D8%A7%D9%84%D9%82%D8%B1%D8%A2%D9%86&modal=create');

  await expect(page.getByTestId('abwab-page')).toBeVisible();
  await expect(page.getByTestId('abwab-page-templates')).toBeVisible();
  await expect(page.getByTestId('abwab-page-add-root')).toHaveCount(0);
  await expect(page.getByTestId('abwab-page-add-root-ghost')).toHaveCount(0);
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);
  await expect(page).not.toHaveURL(/modal=/);

  await page.getByTestId('abwab-page-templates').click();

  await expect(page.getByTestId('abwab-templates-page')).toBeVisible();
  await expect(page.getByTestId('abwab-templates-page-add')).toHaveCount(0);
});

test('a handcrafted anonymous Abwab write remains independently forbidden', async ({ request }) => {
  const response = await request.post(`${API_BASE}/api/abwab/sections`, {
    data: { name: 'phase-9-anonymous-write-must-not-persist' },
  });

  expect(response.status()).toBe(401);
  await expect(response.json()).resolves.toMatchObject({ isSuccess: false });
});
