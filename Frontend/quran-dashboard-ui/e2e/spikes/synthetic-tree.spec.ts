import { expect, test } from '@playwright/test';

import { expectVirtualizedWindow } from '../harness/virtualization';
import { buildSyntheticTreeFixture } from './synthetic-tree.fixture';

// Bounded synthetic-tree spike (FR-022, §14.1/§15.3). It measures how the browser renders and scrolls a
// large virtualized tree of GENERIC synthetic nodes (id/depth/label only) — it freezes NO domain DTO and
// is not a domain test. The recorded render/scroll timings and the rendered-window bound are attached to
// the report so later domain tree Kits (029+) inherit a proven perf/virtualization baseline.
const NODE_COUNT = 2500; // within the mandated 2,000–3,000 bound
const RENDERED_WINDOW_CEILING = 80; // visible window + overscan; far below NODE_COUNT proves virtualization

test('renders a bounded window over a large synthetic tree and records perf', async ({ page }, testInfo) => {
  const { html, nodeCount } = buildSyntheticTreeFixture({ nodeCount: NODE_COUNT });
  expect(nodeCount).toBeGreaterThanOrEqual(2000);
  expect(nodeCount).toBeLessThanOrEqual(3000);

  const renderStart = Date.now();
  await page.setContent(html);
  const viewport = page.locator('[data-testid=tree-viewport]');
  await viewport.waitFor();
  const rows = page.locator('[data-testid=tree-row]');
  await expect.poll(async () => rows.count()).toBeGreaterThan(0);
  const initialRenderMs = Date.now() - renderStart;

  const reportedTotal = Number(await viewport.getAttribute('data-total'));
  expect(reportedTotal).toBe(nodeCount);

  // Virtualization: only a bounded window of the 2,500 nodes is ever in the DOM.
  await expectVirtualizedWindow(rows, {
    renderedAtMost: RENDERED_WINDOW_CEILING,
    totalAtLeast: 2000,
    total: reportedTotal,
  });

  const firstIndexBefore = await rows.first().getAttribute('data-index');

  const scrollStart = Date.now();
  await viewport.evaluate((el) => {
    el.scrollTop = el.scrollHeight * 0.6;
  });
  await expect
    .poll(async () => rows.first().getAttribute('data-index'))
    .not.toBe(firstIndexBefore);
  const scrollUpdateMs = Date.now() - scrollStart;

  // After a deep scroll the window is still bounded and has advanced past the top rows.
  const renderedAfterScroll = await rows.count();
  expect(renderedAfterScroll).toBeLessThanOrEqual(RENDERED_WINDOW_CEILING);
  expect(Number(await rows.first().getAttribute('data-index'))).toBeGreaterThan(0);

  const metrics = {
    nodeCount,
    renderedWindowMax: RENDERED_WINDOW_CEILING,
    renderedAfterScroll,
    initialRenderMs,
    scrollUpdateMs,
  };
  testInfo.annotations.push({ type: 'synthetic-tree-spike', description: JSON.stringify(metrics) });
  await testInfo.attach('synthetic-tree-metrics.json', {
    body: JSON.stringify(metrics, null, 2),
    contentType: 'application/json',
  });
});
