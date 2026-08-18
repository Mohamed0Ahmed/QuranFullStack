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

  const firstDoor = page.locator('[data-testid^="abwab-tree-row-"]').first();
  await expect(firstDoor).toBeVisible();
  await firstDoor.click({ button: 'right' });
  await page.getByTestId('abwab-page-ctx-inclusions').click();

  await expect(page.getByTestId('abwab-inclusions-modal')).toBeVisible();
  await expect(page.getByTestId('abwab-inclusions-modal-submit')).toHaveCount(0);
  await expect(page.locator('[data-testid^="abwab-inclusions-modal-detach-"]')).toHaveCount(0);
  await page.getByTestId('abwab-inclusions-modal-close').click();

  await page.getByTestId('abwab-page-templates').click();

  await expect(page.getByTestId('abwab-templates-page')).toBeVisible();
  await expect(page.getByTestId('abwab-templates-page-add')).toHaveCount(0);
});

test('a handcrafted anonymous Abwab write remains independently forbidden', async ({ request }) => {
  const response = await request.post(`${API_BASE}/api/abwab/doors/999999/inclusions`, {
    data: {
      expectedTargetDoorVersion: 0,
      sourceDoorIds: [999998],
    },
  });

  expect(response.status()).toBe(401);
  await expect(response.json()).resolves.toMatchObject({ isSuccess: false });
});
