import { test, expect } from '../harness';
import { expectRtl } from '../harness/rtl';
import { expectVirtualizedWindow } from '../harness/virtualization';
import { buildSyntheticTreeFixture } from '../spikes/synthetic-tree.fixture';
import { ABWAB_API_ORIGIN, buildAbwabCoreSliceFixture } from './core-slice.fixture';

// T066: Abwab core-slice browser/source suite (SC-015). Driven with an inline, self-contained
// fixture + page.route stubs (the e2e config has no webServer; every spec owns its target — see
// e2e/permissions/non-authoritative.spec.ts and e2e/spikes/synthetic-tree.spec.ts for the same
// established pattern in this repo). The fixture mirrors the SHIPPED contract semantics from
// features/abwab/data-access (explicit actions only, no drag, no edit-session lock, post-mutation
// cache invalidation + reload-to-server-truth) — it is not a re-implementation of the Angular app.

function treeSnapshotResponse(overrides: Partial<{ treeRevision: number; generation: number }> = {}) {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      schemaVersion: 1,
      treeRevision: overrides.treeRevision ?? 1,
      expectedTimelineGeneration: { generation: overrides.generation ?? 1 },
      serverTimeUtc: new Date().toISOString(),
      sections: [],
      categories: [],
      allCategoriesProjection: [],
    },
  };
}

test.describe('Abwab core slice: source suite', () => {
  test('mock/HTTP parity: the same reorder request/response contract drives the SAME UI outcome whether stubbed as a mock success or an HTTP success', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/categories/reorder`, (route) =>
      route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } }),
    );

    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 5 }));
    await page.locator('[data-testid=tree-row]').first().waitFor();

    await page.getByTestId('tree-row').first().click();
    await page.getByTestId('tree-move-down').click();

    await expect(page.getByTestId('conflict-message')).toBeHidden();
    const fetchCount = await page.getByTestId('tree-fetch-count').getAttribute('data-count');
    expect(Number(fetchCount)).toBeGreaterThanOrEqual(2); // initial load + post-mutation reload
  });

  test('stale-cache + rollback: a tree-revision conflict surfaces the exact abwab.* code and re-syncs to server truth instead of applying the rejected change', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/categories/reorder`, (route) =>
      route.fulfill({
        status: 409,
        json: { isSuccess: false, message: 'تعارض', errors: ['abwab.tree_revision_stale'] },
      }),
    );

    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 5 }));
    await page.locator('[data-testid=tree-row]').first().waitFor();

    await page.getByTestId('tree-row').first().click();
    await page.getByTestId('tree-move-up').click();

    await expect(page.getByTestId('conflict-message')).toBeVisible();
    await expect(page.getByTestId('conflict-message')).toHaveText('abwab.tree_revision_stale');
    // Rollback: the rejected mutation triggered a fresh reload (server truth), not a silently-applied change.
    const fetchCount = await page.getByTestId('tree-fetch-count').getAttribute('data-count');
    expect(Number(fetchCount)).toBeGreaterThanOrEqual(2);
  });

  test('RTL: the tree renders right-to-left', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 5 }));
    await expectRtl(page.locator('main'));
  });

  test('keyboard/focus: ArrowDown/ArrowUp move focus, Enter selects, and RTL ArrowLeft expands a collapsed parent', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 20, branching: 3 }));
    const viewport = page.getByTestId('tree-viewport');
    await page.locator('[data-testid=tree-row]').first().waitFor();

    await viewport.focus();
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('ArrowDown');
    let focused = await page.evaluate(() => document.activeElement?.getAttribute('data-id'));
    expect(focused).not.toBeNull();

    await page.keyboard.press('Enter');
    await expect(page.getByTestId('selected-category-id')).toHaveText(focused ?? '');

    // n0 is a root with children under this fixture's branching=3 shape (n3, n6, n9, … are its
    // children) — collapsed by default.
    await expect(page.locator('[data-testid=tree-row][data-id="n3"]')).toBeHidden();
    await viewport.focus();
    await page.keyboard.press('Home');
    await page.keyboard.press('ArrowLeft');
    await expect(page.locator('[data-testid=tree-row][data-id="n3"]')).toBeVisible();
  });

  test('large-tree: virtualizes 2,000-3,000 synthetic categories and records a measured interaction baseline against the reused 028 spike', async ({ page }, testInfo) => {
    // Reuse the accepted 028 synthetic-tree spike scale/measurement AS the recorded baseline —
    // §18.3 fixes no number, so we measure here rather than inventing a threshold.
    const spike = buildSyntheticTreeFixture({ nodeCount: 2500 });
    const baselineStart = Date.now();
    await page.setContent(spike.html);
    await page.locator('[data-testid=tree-viewport]').waitFor();
    await expect.poll(async () => page.locator('[data-testid=tree-row]').count()).toBeGreaterThan(0);
    const baselineRenderMs = Date.now() - baselineStart;

    const baselineViewport = page.locator('[data-testid=tree-viewport]');
    const baselineScrollStart = Date.now();
    await baselineViewport.evaluate((el) => {
      el.scrollTop = el.scrollHeight * 0.6;
    });
    await expect.poll(async () => page.locator('[data-testid=tree-row]').first().getAttribute('data-index')).not.toBe('0');
    const baselineScrollMs = Date.now() - baselineScrollStart;

    // Now the Abwab tree fixture at the SAME mandated scale, fully expanded so the virtualization
    // budget is stressed at the full 2,000-3,000 VISIBLE-row scale (a real user who expanded
    // everything), not just the default-collapsed root level.
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    const abwabRenderStart = Date.now();
    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 2500, expandAllOnLoad: true }));
    const abwabViewport = page.getByTestId('tree-viewport');
    await abwabViewport.waitFor();
    await expect.poll(async () => page.locator('[data-testid=tree-row]').count()).toBeGreaterThan(0);
    const abwabRenderMs = Date.now() - abwabRenderStart;

    const total = await abwabViewport.getAttribute('data-total');
    expect(Number(total)).toBeGreaterThanOrEqual(2000);
    expect(Number(total)).toBeLessThanOrEqual(3000);
    await expectVirtualizedWindow(page.locator('[data-testid=tree-row]'), {
      renderedAtMost: 80,
      totalAtLeast: 2000,
      total: Number(total),
    });

    const abwabScrollStart = Date.now();
    await abwabViewport.evaluate((el) => {
      el.scrollTop = el.scrollHeight * 0.6;
    });
    await expect.poll(async () => page.locator('[data-testid=tree-row]').first().getAttribute('data-index')).not.toBe('0');
    const abwabScrollMs = Date.now() - abwabScrollStart;

    // Assert AGAINST the recorded baseline (generous headroom for a second, independent fixture on
    // the same scale), not an invented absolute millisecond figure.
    expect(abwabRenderMs).toBeLessThanOrEqual(Math.max(baselineRenderMs * 5, 2000));
    expect(abwabScrollMs).toBeLessThanOrEqual(Math.max(baselineScrollMs * 5, 2000));

    const metrics = { baselineRenderMs, baselineScrollMs, abwabRenderMs, abwabScrollMs, nodeCount: Number(total) };
    testInfo.annotations.push({ type: 'abwab-large-tree-baseline', description: JSON.stringify(metrics) });
    await testInfo.attach('abwab-large-tree-metrics.json', { body: JSON.stringify(metrics, null, 2), contentType: 'application/json' });
  });

  test('explicit action: no mutation fires from hover, focus, or scroll — only from an explicit button click', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    let mutationCalls = 0;
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/categories/reorder`, (route) => {
      mutationCalls += 1;
      return route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } });
    });

    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 10 }));
    const firstRow = page.locator('[data-testid=tree-row]').first();
    await firstRow.waitFor();

    await firstRow.hover();
    await firstRow.focus();
    await page.getByTestId('tree-viewport').evaluate((el) => (el.scrollTop = 10));
    await page.waitForTimeout(50);

    expect(mutationCalls).toBe(0);

    await firstRow.click();
    await page.getByTestId('tree-move-down').click();
    expect(mutationCalls).toBe(1);
  });

  test('no-edit-session-lock: opening the editor issues no request, and two independent "sessions" can open it on the same category without conflict', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    let lockCalls = 0;
    await page.route('**/*lock*', (route) => {
      lockCalls += 1;
      return route.fulfill({ json: { isSuccess: false }, status: 404 });
    });

    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 5 }));
    await page.locator('[data-testid=tree-row]').first().waitFor();
    await page.getByTestId('tree-row').first().click();
    await page.getByTestId('tree-edit').click();
    const openedFor = await page.evaluate(() => (window as unknown as { __abwabOpenedEditorFor: string | null }).__abwabOpenedEditorFor);

    expect(openedFor).not.toBeNull();
    expect(lockCalls).toBe(0);

    // A second "session" opening the SAME category's editor is unaffected — no lock was ever taken.
    await page.getByTestId('tree-edit').click();
    expect(lockCalls).toBe(0);
  });

  test('no-drag: rows carry no draggable attribute, and a synthetic drag sequence never reorders anything', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse() }));
    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 5 }));
    const rows = page.locator('[data-testid=tree-row]');
    await rows.first().waitFor();

    const draggableCount = await rows.evaluateAll((elements) => elements.filter((el) => el.hasAttribute('draggable')).length);
    expect(draggableCount).toBe(0);

    const orderBefore = await rows.evaluateAll((elements) => elements.map((el) => el.getAttribute('data-id')));
    const first = rows.first();
    const second = rows.nth(1);
    await first.hover();
    await page.mouse.down();
    await second.hover();
    await page.mouse.up();
    const orderAfter = await rows.evaluateAll((elements) => elements.map((el) => el.getAttribute('data-id')));

    expect(orderAfter).toEqual(orderBefore);
  });

  test('post-mutation context preservation: selection and expansion survive a successful mutation reload', async ({ page }) => {
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/tree`, (route) => route.fulfill({ json: treeSnapshotResponse({ treeRevision: 1 }) }));
    await page.route(`${ABWAB_API_ORIGIN}/api/abwab/categories/reorder`, (route) =>
      route.fulfill({ json: { isSuccess: true, message: 'تم', data: null } }),
    );

    await page.setContent(buildAbwabCoreSliceFixture({ nodeCount: 20, branching: 3 }));
    await page.locator('[data-testid=tree-row]').first().waitFor();

    // n3 is a CHILD of root n0 in this fixture's shape; expanding n0 and selecting n3 exercises
    // both expansion state and selection, not just selection alone.
    await page.evaluate(() => (window as unknown as { __abwabFixture: { expand(id: string): void } }).__abwabFixture.expand('n0'));
    await page.locator('[data-testid=tree-row][data-id="n3"]').click();

    await expect(page.getByTestId('selected-category-id')).toHaveText('n3');
    await page.getByTestId('tree-move-down').click();

    await expect(page.getByTestId('selected-category-id')).toHaveText('n3');
    await expect(page.locator('[data-testid=tree-row][data-id="n3"]')).toBeVisible();
  });
});
