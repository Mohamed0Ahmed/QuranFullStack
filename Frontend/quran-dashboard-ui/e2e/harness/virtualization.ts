import { expect, type Locator } from '@playwright/test';

// Large trees/lists must render a bounded window of nodes regardless of total size. The harness asserts
// the rendered node count stays under a ceiling while the reported total is far larger.
export async function expectVirtualizedWindow(
  renderedItems: Locator,
  options: { renderedAtMost: number; totalAtLeast: number; total: number },
): Promise<void> {
  expect(options.total).toBeGreaterThanOrEqual(options.totalAtLeast);
  expect(await renderedItems.count()).toBeLessThanOrEqual(options.renderedAtMost);
}
