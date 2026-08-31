import AxeBuilder from '@axe-core/playwright';
import type { Page, TestInfo } from '@playwright/test';

import { expect } from './app-test';

export async function expectNoBlockingAccessibilityViolations(
  page: Page,
  testInfo: TestInfo,
): Promise<void> {
  const results = await new AxeBuilder({ page }).analyze();
  const observations = results.violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact ?? 'unknown',
    help: violation.help,
    nodeCount: violation.nodes.length,
    targets: violation.nodes.flatMap((node) => node.target.map((target) => String(target))),
  }));
  await testInfo.attach('accessibility-observations', {
    body: Buffer.from(`${JSON.stringify(observations, null, 2)}\n`),
    contentType: 'application/json',
  });
  const blocking = results.violations
    .filter((violation) => violation.impact === 'serious' || violation.impact === 'critical')
    .map((violation) => ({
      help: violation.help,
      id: violation.id,
      impact: violation.impact,
      nodeCount: violation.nodes.length,
      targets: violation.nodes.flatMap((node) => node.target.map((target) => String(target))),
    }));

  expect(
    blocking,
    `serious/critical accessibility violations:\n${JSON.stringify(blocking, null, 2)}`,
  ).toEqual([]);
}
