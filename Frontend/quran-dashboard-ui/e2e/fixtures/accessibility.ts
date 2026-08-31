import AxeBuilder from '@axe-core/playwright';
import type { Page, TestInfo } from '@playwright/test';

import { expect } from './app-test';

interface AccessibilityObservation {
  id: string;
  impact: string;
  help: string;
  nodeCount: number;
  targets: string[];
}

export interface AccessibilityAudit {
  expectNoBlockingViolations(page: Page): Promise<void>;
  attachObservations(): Promise<void>;
}

export async function expectNoBlockingAccessibilityViolations(
  page: Page,
  testInfo: TestInfo,
): Promise<void> {
  const audit = createAccessibilityAudit(testInfo);
  try {
    await audit.expectNoBlockingViolations(page);
  } finally {
    await audit.attachObservations();
  }
}

export function createAccessibilityAudit(testInfo: TestInfo): AccessibilityAudit {
  const observations: AccessibilityObservation[] = [];
  let attached = false;

  return {
    async expectNoBlockingViolations(page: Page): Promise<void> {
      const scan = await scanAccessibility(page);
      observations.push(...scan.observations);
      expect(
        scan.blocking,
        `serious/critical accessibility violations:\n${JSON.stringify(scan.blocking, null, 2)}`,
      ).toEqual([]);
    },
    async attachObservations(): Promise<void> {
      if (attached) {
        throw new Error('Accessibility observations may be attached only once per test result.');
      }
      attached = true;
      await testInfo.attach('accessibility-observations', {
        body: Buffer.from(`${JSON.stringify(observations, null, 2)}\n`),
        contentType: 'application/json',
      });
    },
  };
}

async function scanAccessibility(page: Page): Promise<{
  observations: AccessibilityObservation[];
  blocking: AccessibilityObservation[];
}> {
  const results = await new AxeBuilder({ page }).analyze();
  const observations = results.violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact ?? 'unknown',
    help: violation.help,
    nodeCount: violation.nodes.length,
    targets: violation.nodes.flatMap((node) => node.target.map((target) => String(target))),
  }));
  const blocking = results.violations
    .filter((violation) => violation.impact === 'serious' || violation.impact === 'critical')
    .map((violation) => ({
      help: violation.help,
      id: violation.id,
      impact: violation.impact ?? 'unknown',
      nodeCount: violation.nodes.length,
      targets: violation.nodes.flatMap((node) => node.target.map((target) => String(target))),
    }));
  return { observations, blocking };
}
