import { test, expect } from '../harness';
import { expectRtl } from '../harness/rtl';
import { ABWAB_API_ORIGIN, RELATIONSHIP_CONFLICT_MESSAGES, buildAbwabRelationshipsFixture } from './relationships-slice.fixture';

// T021: Abwab relationship-slice browser suite. See relationships-slice.fixture.ts for exactly what
// this layer proves and what it defers to the component/facade unit specs.

function relationshipsResponse(count: number, generation = 1) {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      schemaVersion: 1,
      categoryId: 'category-a',
      expectedTimelineGeneration: { generation },
      serverTimeUtc: new Date().toISOString(),
      relationships: Array.from({ length: count }, (_, index) => ({
        categoryRelationshipId: `relationship-${index}`,
        relationshipType: 0,
        isDirectional: false,
        from: { categoryId: 'category-a', name: 'باب أ', path: [], isDeleted: false },
        to: { categoryId: `category-${index}`, name: `باب ${index}`, path: [], isDeleted: false },
        isDeleted: false,
        version: 1,
      })),
    },
  };
}

async function stubList(page: import('@playwright/test').Page, count: number) {
  await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships/category-a*`, (route) =>
    route.fulfill({ json: relationshipsResponse(count) }),
  );
}

async function fetchCount(page: import('@playwright/test').Page): Promise<number> {
  return Number(await page.getByTestId('relationships-fetch-count').getAttribute('data-count'));
}

test.describe('Abwab relationship slice: browser suite', () => {
  test('explicit action: adding a relationship is a button click, and the list re-syncs from the server exactly once afterwards', async ({ page }) => {
    await stubList(page, 2);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) =>
      route.fulfill({ json: { isSuccess: true, message: 'تم', data: { relationshipId: 'relationship-9' } } }),
    );

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();
    expect(await fetchCount(page)).toBe(1);

    await page.getByTestId('relationship-add').click();

    await expect(page.getByTestId('relationships-conflict')).toBeHidden();
    await expect.poll(() => fetchCount(page)).toBe(2); // initial load + exactly one post-mutation reload
  });

  test('wire contract: the submitted RelationshipType is an INTEGER, so a directional edge is not silently rejected', async ({ page }) => {
    await stubList(page, 1);
    let submitted: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: { relationshipId: 'relationship-9' } } });
    });

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-type').selectOption('2');
    await page.getByTestId('relationship-add').click();
    await expect.poll(() => fetchCount(page)).toBe(2);

    expect(submitted).not.toBeNull();
    expect(typeof submitted!['relationshipType']).toBe('number');
    expect(submitted!['relationshipType']).toBe(2);
  });

  test('direction: choosing «هذا الباب هو الأخص» submits the routed door as the NARROWER end', async ({ page }) => {
    await stubList(page, 1);
    let submitted: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: { relationshipId: 'relationship-9' } } });
    });

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-type').selectOption('2');
    await page.getByTestId('relationship-direction').selectOption('routed-is-narrower');
    await page.getByTestId('relationship-endpoint').selectOption('category-b');
    await page.getByTestId('relationship-add').click();
    await expect.poll(() => fetchCount(page)).toBe(2);

    expect(submitted!['firstCategoryId']).toBe('category-b');
    expect(submitted!['secondCategoryId']).toBe('category-a');
  });

  test('stale-cache + rollback: a duplicate conflict surfaces the ARABIC message (never the raw code) and re-syncs to server truth', async ({ page }) => {
    await stubList(page, 1);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) =>
      route.fulfill({
        status: 409,
        json: { isSuccess: false, message: 'تعارض', errors: ['abwab.relationship_duplicate'] },
      }),
    );

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-add').click();

    const conflict = page.getByTestId('relationships-conflict');
    await expect(conflict).toBeVisible();
    await expect(conflict).toHaveText(RELATIONSHIP_CONFLICT_MESSAGES['abwab.relationship_duplicate']);
    await expect(conflict).not.toContainText('abwab.');
    await expect(page.getByTestId('relationship-list')).toHaveAttribute('data-total', '1');
    await expect.poll(() => fetchCount(page)).toBe(2);
  });

  test('cycle conflict: a rejected Broader/Narrower edge surfaces the cycle message and applies nothing locally', async ({ page }) => {
    await stubList(page, 2);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) =>
      route.fulfill({ status: 409, json: { isSuccess: false, message: 'تعارض', errors: ['abwab.relationship_cycle'] } }),
    );

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-type').selectOption('2');
    await page.getByTestId('relationship-add').click();

    await expect(page.getByTestId('relationships-conflict')).toHaveText(
      RELATIONSHIP_CONFLICT_MESSAGES['abwab.relationship_cycle'],
    );
    await expect(page.getByTestId('relationships-conflict')).not.toContainText('abwab.');
    await expect(page.getByTestId('relationship-list')).toHaveAttribute('data-total', '2');
  });

  test('RTL: the relationship panel renders right-to-left', async ({ page }) => {
    await stubList(page, 2);
    await page.setContent(buildAbwabRelationshipsFixture());
    await expectRtl(page.locator('main'));
  });

  test('keyboard/focus: every row action is reachable by native tab order, in RTL document order', async ({ page }) => {
    await stubList(page, 2);
    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-add').focus();

    const reached: (string | null)[] = [];
    for (let step = 0; step < 3; step += 1) {
      await page.keyboard.press('Tab');
      reached.push(await page.evaluate(() => document.activeElement?.getAttribute('data-testid') ?? null));
    }

    expect(reached).toEqual(['relationship-select', 'relationship-edit', 'relationship-delete']);
  });

  test('no-drag: rows carry no draggable attribute and a synthetic drag sequence never reorders or mutates anything', async ({ page }) => {
    await stubList(page, 3);
    let mutationCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships`, (route) => {
      mutationCalls += 1;
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: { relationshipId: 'relationship-9' } } });
    });

    await page.setContent(buildAbwabRelationshipsFixture());
    const rows = page.locator('[data-testid=relationship-row]');
    await rows.first().waitFor();

    const draggableCount = await rows.evaluateAll((elements) => elements.filter((el) => el.hasAttribute('draggable')).length);
    expect(draggableCount).toBe(0);

    const orderBefore = await rows.evaluateAll((elements) => elements.map((el) => el.getAttribute('data-id')));
    await rows.first().hover();
    await page.mouse.down();
    await rows.nth(2).hover();
    await page.mouse.up();
    const orderAfter = await rows.evaluateAll((elements) => elements.map((el) => el.getAttribute('data-id')));

    expect(orderAfter).toEqual(orderBefore);
    expect(mutationCalls).toBe(0);
  });

  test('explicit action: no mutation fires from hover, focus, or selection — only from an explicit button click', async ({ page }) => {
    await stubList(page, 2);
    let deleteCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships/relationship-0`, (route) => {
      deleteCalls += 1;
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabRelationshipsFixture());
    const firstRow = page.locator('[data-testid=relationship-row]').first();
    await firstRow.waitFor();

    await firstRow.hover();
    await firstRow.click();
    await page.getByTestId('relationship-select').first().click();
    await page.waitForTimeout(50);
    expect(deleteCalls).toBe(0);

    await page.getByTestId('relationship-delete').first().click();
    await expect.poll(() => deleteCalls).toBe(1);
  });

  test('post-mutation context preservation: the selected relationship and typed input survive a mutation reload', async ({ page }) => {
    await stubList(page, 3);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/relationships/relationship-1`, (route) =>
      route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } }),
    );

    await page.setContent(buildAbwabRelationshipsFixture());
    await page.locator('[data-testid=relationship-row]').first().waitFor();

    await page.getByTestId('relationship-endpoint').selectOption('category-z');
    await page.locator('[data-testid=relationship-row][data-id="relationship-1"] [data-testid=relationship-select]').click();
    await expect(page.getByTestId('selected-relationship-id')).toHaveText('relationship-1');

    await page.locator('[data-testid=relationship-row][data-id="relationship-1"] [data-testid=relationship-delete]').click();

    await expect(page.getByTestId('selected-relationship-id')).toHaveText('relationship-1');
    await expect(page.getByTestId('relationship-endpoint')).toHaveValue('category-z');
  });

  test('distinct states: an empty relationship list renders an empty state rather than a silent blank', async ({ page }) => {
    await stubList(page, 0);

    await page.setContent(buildAbwabRelationshipsFixture());

    await expect(page.getByTestId('relationships-status')).toHaveText('empty');
    await expect(page.getByTestId('relationship-list')).toHaveAttribute('data-total', '0');
  });
});
