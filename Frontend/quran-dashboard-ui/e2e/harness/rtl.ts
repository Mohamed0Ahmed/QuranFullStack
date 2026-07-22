import { expect, type Locator } from '@playwright/test';

// The product is Arabic-first; every Kit's shell must resolve to a right-to-left flow.
export async function expectRtl(target: Locator): Promise<void> {
  const direction = await target.evaluate((element) => getComputedStyle(element).direction);
  expect(direction).toBe('rtl');
}
