import type { Page } from '@playwright/test';
import { expect, test } from './fixtures/abwab';

// Global-order independence flow: the superset («كل الأبواب») and a section
// each carry their own root order — a write in one space must never move the
// other. Asserts relative order among the sandbox's own doors only, never an absolute global
// number and never a global count (TESTING_STRATEGY.md §6).

async function readOrder(page: Page, doorId: number): Promise<number> {
  const text = await page.getByTestId(`abwab-tree-order-${doorId}`).innerText();
  return Number(text);
}

test('a superset (Global) reorder leaves the section order untouched, and a section reorder leaves the superset order untouched', async ({
  page,
  abwabSandbox,
}) => {
  const doorA = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('order-a') });
  const doorB = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('order-b') });
  const doorC = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('order-c') });

  // The superset — no `section` param, so `orderScope` resolves to 'global' (abwab-page.component.ts).
  // Doors are appended last on create, so creation order alone guarantees A < B < C here.
  await page.goto('/abwab');
  const globalA = await readOrder(page, doorA.id);
  expect(globalA).toBeLessThan(await readOrder(page, doorB.id));
  expect(await readOrder(page, doorB.id)).toBeLessThan(await readOrder(page, doorC.id));

  // Move C past A — a Global-scope write (position is C's target index in the whole live-root set).
  await page.getByTestId(`abwab-tree-order-${doorC.id}`).click();
  await page.getByTestId(`abwab-tree-order-input-${doorC.id}`).fill(String(globalA));
  await page.getByTestId(`abwab-tree-order-input-${doorC.id}`).press('Enter');

  await expect(page.getByTestId(`abwab-tree-order-${doorC.id}`)).toHaveText(String(globalA));
  const globalAAfter = await readOrder(page, doorA.id);
  expect(await readOrder(page, doorC.id)).toBeLessThan(globalAAfter);
  expect(globalAAfter).toBeLessThan(await readOrder(page, doorB.id));

  // The section view's own order_value is untouched by the Global write above.
  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await expect(page.getByTestId(`abwab-tree-order-${doorA.id}`)).toHaveText('1');
  await expect(page.getByTestId(`abwab-tree-order-${doorB.id}`)).toHaveText('2');
  await expect(page.getByTestId(`abwab-tree-order-${doorC.id}`)).toHaveText('3');

  // Reorder within the section — a Section-scope write. Move B ahead of A.
  await page.getByTestId(`abwab-tree-order-${doorB.id}`).click();
  await page.getByTestId(`abwab-tree-order-input-${doorB.id}`).fill('1');
  await page.getByTestId(`abwab-tree-order-input-${doorB.id}`).press('Enter');

  await expect(page.getByTestId(`abwab-tree-order-${doorB.id}`)).toHaveText('1');
  await expect(page.getByTestId(`abwab-tree-order-${doorA.id}`)).toHaveText('2');
  await expect(page.getByTestId(`abwab-tree-order-${doorC.id}`)).toHaveText('3');

  // The superset's relative order from the earlier Global write (C, A, B) is untouched.
  await page.goto('/abwab');
  const finalC = await readOrder(page, doorC.id);
  const finalA = await readOrder(page, doorA.id);
  expect(finalC).toBeLessThan(finalA);
  expect(finalA).toBeLessThan(await readOrder(page, doorB.id));
});
