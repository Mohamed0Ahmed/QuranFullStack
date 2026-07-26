import { test, expect } from '../harness';
import { expectRtl } from '../harness/rtl';
import { ABWAB_API_ORIGIN, ABWAB_CONFLICT_MESSAGES, buildAbwabTemplatesFixture } from './templates-slice.fixture';

// T053: Abwab template-slice browser suite. See templates-slice.fixture.ts for exactly what this
// layer proves and what it defers to the component/facade unit specs.
//
// Every read counts: the facade re-reads the LIST and the DETAIL after each outcome, so one initial
// load is 2 fetches and each mutation adds 2 more.
const INITIAL_FETCHES = 2;
const AFTER_ONE_MUTATION = 4;

function templateResponse(nodeCount: number, templateRevision = 0, generation = 1) {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      schemaVersion: 1,
      template: {
        doorTemplateId: 'template-1',
        name: 'قالب الأبواب',
        description: null,
        templateRevision,
        isDeleted: false,
        version: 1,
        nodeCount,
      },
      nodes: Array.from({ length: nodeCount }, (_, index) => ({
        templateNodeId: `node-${index}`,
        parentTemplateNodeId: null,
        name: `عقدة ${index}`,
        representativeQuranExcerpt: null,
        description: null,
        siblingOrder: index,
        isDeleted: false,
        version: 1,
        aliases:
          index === 0
            ? [
                { templateNodeSearchAliasId: 'alias-0', value: 'مرادف فعّال', isDeleted: false, version: 3 },
                { templateNodeSearchAliasId: 'alias-1', value: 'مرادف مُزال', isDeleted: true, version: 5 },
              ]
            : [],
      })),
      expectedTimelineGeneration: { generation },
      treeRevision: 1,
      serverTimeUtc: new Date().toISOString(),
    },
  };
}

async function stubTemplate(page: import('@playwright/test').Page, nodeCount: number) {
  await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates?*`, (route) =>
    route.fulfill({ json: { isSuccess: true, message: 'تم', data: { schemaVersion: 1, templates: [], expectedTimelineGeneration: { generation: 1 }, serverTimeUtc: new Date().toISOString() } } }),
  );
  await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1?*`, (route) =>
    route.fulfill({ json: templateResponse(nodeCount) }),
  );
}

async function fetchCount(page: import('@playwright/test').Page): Promise<number> {
  return Number(await page.getByTestId('templates-fetch-count').getAttribute('data-count'));
}

test.describe('Abwab template slice: browser suite', () => {
  test('explicit save: adding a node is a button click, and the editor re-syncs from the server exactly once afterwards', async ({ page }) => {
    await stubTemplate(page, 2);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/nodes`, (route) =>
      route.fulfill({ json: { isSuccess: true, message: 'تم', data: { templateNodeId: 'node-9' } } }),
    );

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();
    expect(await fetchCount(page)).toBe(INITIAL_FETCHES);

    await page.getByTestId('node-name').fill('عقدة جديدة');
    await page.getByTestId('node-add').click();

    await expect(page.getByTestId('templates-conflict')).toBeHidden();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);
  });

  test('no autosave and no edit-session lock: typing a node name alone fires no request', async ({ page }) => {
    await stubTemplate(page, 2);
    let mutationCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/nodes`, (route) => {
      mutationCalls += 1;
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: { templateNodeId: 'node-9' } } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.getByTestId('node-name').fill('مسودة لم تُحفظ');
    await page.getByTestId('node-name').blur();

    expect(mutationCalls).toBe(0);
    expect(await fetchCount(page)).toBe(INITIAL_FETCHES);
  });

  test('explicit ordering: move-up posts the recomputed sibling order, never a drop position', async ({ page }) => {
    await stubTemplate(page, 3);
    let submitted: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/nodes/reorder`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.locator('[data-testid=node-move-up]').nth(1).click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);

    expect(submitted).not.toBeNull();
    expect(submitted!['orderedTemplateNodeIds']).toEqual(['node-1', 'node-0', 'node-2']);
    expect(submitted!['expectedTemplateRevision']).toBe(0);
  });

  test('explicit reparent: the destination is chosen inside the open edit form and submitted as a named parent id', async ({ page }) => {
    await stubTemplate(page, 2);
    let submitted: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/nodes/node-1/reparent`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.locator('[data-testid=node-edit]').nth(1).click();
    await page.getByTestId('node-edit-parent').selectOption('node-0');
    await page.getByTestId('node-reparent').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);

    expect(submitted!['newParentTemplateNodeId']).toBe('node-0');
  });

  test('stale-cache + rollback: a template_cycle conflict surfaces the ARABIC message (never the raw code) and re-syncs to server truth', async ({ page }) => {
    await stubTemplate(page, 2);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/nodes/node-0/reparent`, (route) =>
      route.fulfill({ status: 409, json: { isSuccess: false, message: 'تعارض', errors: ['abwab.template_cycle'] } }),
    );

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.locator('[data-testid=node-edit]').first().click();
    await page.getByTestId('node-reparent').click();

    const conflict = page.getByTestId('templates-conflict');
    await expect(conflict).toBeVisible();
    await expect(conflict).toHaveText(ABWAB_CONFLICT_MESSAGES['abwab.template_cycle']);
    await expect(conflict).not.toContainText('abwab.');
    await expect(page.getByTestId('template-node-list')).toHaveAttribute('data-total', '2');
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);
  });

  test('alias sub-editor: adding a synonym is an explicit save that re-syncs from the server', async ({ page }) => {
    await stubTemplate(page, 2);
    let submitted: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/nodes/node-0/aliases`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: { templateNodeSearchAliasId: 'alias-9' } } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.locator('[data-testid=node-alias-add-open]').first().click();
    await page.getByTestId('node-alias-add-value').fill('مرادف جديد');
    await page.getByTestId('node-alias-add-save').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);

    expect(submitted!['value']).toBe('مرادف جديد');
  });

  // Every alias command carries the ALIAS row's own xmin, never the node's: alias-1 is at version 5
  // while its node is at version 1.
  test('alias sub-editor: remove and restore each send that alias row own expected version', async ({ page }) => {
    await stubTemplate(page, 2);
    let removed: Record<string, unknown> | null = null;
    let restored: Record<string, unknown> | null = null;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/aliases/alias-0`, (route) => {
      removed = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/aliases/alias-1/restore`, (route) => {
      restored = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.getByTestId('node-alias-remove').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);
    expect(removed!['expectedVersion']).toBe(3);

    await page.getByTestId('node-alias-restore').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION + 2);
    expect(restored!['expectedVersion']).toBe(5);
  });

  test('application flow: confirming posts one apply request carrying the chosen target and every expectation', async ({ page }) => {
    await stubTemplate(page, 2);
    let submitted: Record<string, unknown> | null = null;
    let applyCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/apply`, (route) => {
      applyCalls += 1;
      submitted = route.request().postDataJSON();
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.getByTestId('apply-target').selectOption('category-target');
    await page.getByTestId('apply-begin').click();
    expect(applyCalls).toBe(0); // choosing a target alone never applies — the confirm step is explicit

    await page.getByTestId('apply-confirm-yes').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);

    expect(applyCalls).toBe(1);
    expect(submitted!['targetCategoryId']).toBe('category-target');
    expect(submitted!['expectedTreeRevision']).toBe(1);
    expect(submitted!['expectedTemplateRevision']).toBe(0);
  });

  test('application conflict: a protected target surfaces the Arabic protection message and creates nothing locally', async ({ page }) => {
    await stubTemplate(page, 2);
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/apply`, (route) =>
      route.fulfill({ status: 409, json: { isSuccess: false, message: 'تعارض', errors: ['abwab.manual_protection'] } }),
    );

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.getByTestId('apply-target').selectOption('category-protected');
    await page.getByTestId('apply-begin').click();
    await page.getByTestId('apply-confirm-yes').click();

    await expect(page.getByTestId('templates-conflict')).toHaveText(ABWAB_CONFLICT_MESSAGES['abwab.manual_protection']);
    await expect(page.getByTestId('template-node-list')).toHaveAttribute('data-total', '2');
  });

  // Context preservation, as the shipped editor defines it: a rejected write leaves the open form
  // exactly as the operator left it, and only an ACCEPTED write closes it.
  test('context preservation: an open edit form survives a conflict and closes only on success', async ({ page }) => {
    await stubTemplate(page, 2);
    let shouldConflict = true;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/nodes/node-0`, (route) =>
      shouldConflict
        ? route.fulfill({ status: 409, json: { isSuccess: false, message: 'تعارض', errors: ['abwab.row_stale'] } })
        : route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } }),
    );

    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    await page.locator('[data-testid=node-edit]').first().click();
    await page.getByTestId('node-edit-name').fill('اسم لم يُحفظ');
    await page.getByTestId('node-edit-save').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION);

    await expect(page.getByTestId('template-node-edit-form')).toBeVisible();
    await expect(page.getByTestId('node-edit-name')).toHaveValue('اسم لم يُحفظ');
    await expect(page.getByTestId('templates-conflict')).toHaveText(ABWAB_CONFLICT_MESSAGES['abwab.row_stale']);

    shouldConflict = false;
    await page.getByTestId('node-edit-save').click();
    await expect.poll(() => fetchCount(page)).toBe(AFTER_ONE_MUTATION + 2);
    await expect(page.getByTestId('template-node-edit-form')).toHaveCount(0);
  });

  test('RTL: the template editor renders right-to-left', async ({ page }) => {
    await stubTemplate(page, 2);
    await page.setContent(buildAbwabTemplatesFixture());
    await expectRtl(page.locator('main'));
  });

  test('keyboard/focus: every row action is reachable by native tab order, in RTL document order', async ({ page }) => {
    await stubTemplate(page, 3);
    await page.setContent(buildAbwabTemplatesFixture());
    await page.locator('[data-testid=template-node-row]').first().waitFor();

    // The MIDDLE row, so neither move button is disabled and the full per-row order is observable.
    await page.locator('[data-testid=node-edit]').nth(1).focus();

    const reached: (string | null)[] = [];
    for (let step = 0; step < 4; step += 1) {
      await page.keyboard.press('Tab');
      reached.push(await page.evaluate(() => document.activeElement?.getAttribute('data-testid') ?? null));
    }

    expect(reached).toEqual(['node-move-up', 'node-move-down', 'node-remove', 'node-alias-add-open']);
  });

  test('no-drag: node rows carry no draggable attribute and a synthetic drag sequence never reorders or mutates anything', async ({ page }) => {
    await stubTemplate(page, 3);
    let mutationCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/templates/template-1/nodes/reorder`, (route) => {
      mutationCalls += 1;
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabTemplatesFixture());
    const rows = page.locator('[data-testid=template-node-row]');
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
});
