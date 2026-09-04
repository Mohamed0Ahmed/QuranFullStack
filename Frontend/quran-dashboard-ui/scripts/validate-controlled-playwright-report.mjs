import { lstatSync, readFileSync, readdirSync } from 'node:fs';
import { basename, relative, resolve } from 'node:path';

import {
  APPROVED_DIAGNOSTIC_FILES,
  validateRetainedDiagnostic,
} from './structured-playwright-reporter.mjs';

const CHILD_STATUSES = new Set(['passed', 'failed', 'timedOut', 'skipped', 'interrupted']);
const RAW_FILE_EXTENSION = /\.(?:bak|dump|har|html|key|md|pem|sql|trace|zip)$/i;

export function validateControlledPlaywrightRun(runDirectory, expectedStatus) {
  const errors = [];
  const reportPath = resolve(runDirectory, 'playwright-run.json');
  const report = readJson(reportPath, 'controlled-run-report-invalid', errors);
  const expectedRunId = basename(runDirectory);
  if (
    report?.schemaVersion !== 1
    || report.kind !== 'complete-playwright'
    || report.runId !== expectedRunId
    || !['passed', 'failed'].includes(report.status)
    || report.status !== expectedStatus
    || !Array.isArray(report.children)
    || !Array.isArray(report.tests)
    || !Number.isInteger(report.declaredTestCount)
    || report.declaredTestCount !== report.tests.length
  ) {
    errors.push('controlled-run-contract-invalid');
  }

  const tests = Array.isArray(report?.tests) ? report.tests : [];
  validatePhases(report?.provisioningPhases, [
    'dependencyProvisioning',
    'chromiumProvisioning',
    'certificateProvisioning',
    'buildProvisioning',
  ], errors);
  if (report?.provisioningPhases?.some((phase) => phase?.status !== 'passed')) {
    errors.push('controlled-provisioning-phase-failed');
  }
  if (new Set(tests.map((test) => test?.id)).size !== tests.length) {
    errors.push('controlled-run-test-ids-invalid');
  }
  if (tests.some((test) => !isValidTest(test))) errors.push('controlled-run-tests-invalid');
  if (!countsMatch(report?.counts, tests)) errors.push('controlled-run-counts-invalid');

  const childTests = [];
  const children = Array.isArray(report?.children) ? report.children : [];
  for (const child of children) {
    validateChild(runDirectory, child, childTests, errors);
  }
  const childKinds = children.map((child) => child?.kind);
  if (new Set(childKinds).size !== childKinds.length) errors.push('controlled-child-kinds-duplicate');
  const childrenPassed = childKinds.length === 2
    && childKinds.includes('canonical-read')
    && childKinds.includes('stateful')
    && children.every((child) => child?.status === 'passed');
  if ((report?.status === 'passed') !== childrenPassed) errors.push('controlled-run-status-mismatch');
  if (report?.status === 'passed' && tests.some((test) => !['passed', 'skipped'].includes(test.status))) {
    errors.push('controlled-run-passing-tests-mismatch');
  }
  if (JSON.stringify(childTests) !== JSON.stringify(tests)) {
    errors.push('controlled-run-child-tests-mismatch');
  }

  const inventory = evidenceInventory(runDirectory);
  errors.push(...inventory.errors);
  return {
    status: errors.length === 0 ? 'passed' : 'failed',
    checkIds: [...new Set(errors)],
    path: reportPath,
    tests,
    declaredTestCount: report?.declaredTestCount,
    runStatus: report?.status,
  };
}

function validateChild(runDirectory, child, collectedTests, errors) {
  if (
    !child
    || !['canonical-read', 'stateful'].includes(child.kind)
    || !['passed', 'failed'].includes(child.status)
    || !Number.isFinite(child.durationMs)
    || child.durationMs < 0
  ) {
    errors.push('controlled-child-kind-invalid');
    return;
  }
  const expectedReport = child.kind === 'canonical-read'
    ? 'canonical/canonical-results.json'
    : 'stateful/stateful-results.json';
  if (child.report !== expectedReport) errors.push(`controlled-child-report-invalid:${child.kind}`);
  const actual = readJson(resolve(runDirectory, expectedReport), 'controlled-child-report-missing', errors);
  const { report: _report, ...embedded } = child;
  if (JSON.stringify(actual) !== JSON.stringify(embedded)) {
    errors.push(`controlled-child-report-mismatch:${child.kind}`);
  }

  if (child.kind === 'canonical-read') {
    validatePhases(child.phases, [
      'capabilityInspection',
      'applicationStartup',
      'testExecution',
      'applicationShutdown',
    ], errors);
    validateInspection(child.inspection, errors);
    collectPlaywrightTests(
      resolve(runDirectory, 'canonical', child.evidence ?? ''),
      child.runId,
      collectedTests,
      errors,
    );
    return;
  }

  if (!Array.isArray(child.scenarios)) {
    errors.push('controlled-stateful-scenarios-invalid');
    return;
  }
  for (const scenario of child.scenarios) {
    const required = ['lockAcquisition', 'applicationStartup', 'testExecution', 'applicationShutdown', 'lockRelease'];
    if (scenario?.policy === 'mutating') required.push('initialReset', 'finalReset');
    validatePhases(scenario?.phases, required, errors);
    validateInspection(scenario?.inspection, errors);
    collectPlaywrightTests(
      resolve(runDirectory, 'stateful', scenario?.evidence ?? ''),
      scenario?.runId,
      collectedTests,
      errors,
    );
  }
}

function collectPlaywrightTests(path, runId, collectedTests, errors) {
  const evidence = readJson(path, 'controlled-child-evidence-invalid', errors);
  if (
    evidence?.schemaVersion !== 1
    || evidence.runId !== runId
    || !Array.isArray(evidence.tests)
    || evidence.declaredTestCount !== evidence.tests.length
    || !countsMatch(evidence.counts, evidence.tests)
  ) {
    errors.push('controlled-child-evidence-contract-invalid');
    return;
  }
  collectedTests.push(...evidence.tests);
}

function validatePhases(phases, required, errors) {
  if (!Array.isArray(phases)) {
    errors.push('controlled-phases-invalid');
    return;
  }
  for (const name of required) {
    const matches = phases.filter((phase) => phase?.name === name);
    if (
      matches.length !== 1
      || !['passed', 'failed'].includes(matches[0].status)
      || !Number.isFinite(matches[0].durationMs)
      || matches[0].durationMs < 0
    ) {
      errors.push(`controlled-phase-invalid:${name}`);
    }
  }
}

function validateInspection(inspection, errors) {
  if (
    inspection?.status !== 'passed'
    || inspection.unsafeScreenshot !== false
    || !Array.isArray(inspection.invalidDiagnosticFiles)
    || inspection.invalidDiagnosticFiles.length !== 0
    || !Array.isArray(inspection.symlinks)
    || inspection.symlinks.length !== 0
    || !Array.isArray(inspection.unexpectedFiles)
    || inspection.unexpectedFiles.length !== 0
  ) {
    errors.push('controlled-evidence-inspection-invalid');
  }
}

function evidenceInventory(root, current = root) {
  const errors = [];
  try {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const path = resolve(current, entry.name);
      const details = lstatSync(path);
      const name = relative(root, path);
      if (details.isSymbolicLink()) {
        errors.push(`controlled-evidence-symlink:${name}`);
      } else if (details.isDirectory()) {
        errors.push(...evidenceInventory(root, path).errors);
      } else if (!details.isFile() || RAW_FILE_EXTENSION.test(name) || !isAllowedFile(name, path)) {
        errors.push(`controlled-evidence-file-invalid:${name}`);
      }
    }
  } catch {
    errors.push('controlled-evidence-inventory-invalid');
  }
  return { errors };
}

function isAllowedFile(name, path) {
  if (name === 'playwright-run.json') return true;
  if (/^(?:canonical\/canonical-results|stateful\/stateful-results)\.json$/.test(name)) return true;
  if (/^(?:canonical|stateful\/child-\d{3})\/evidence\/(?:application\.log|playwright-results\.json|sanitized-trace\.json)$/.test(name)) {
    return true;
  }
  const match = /^(?:canonical|stateful\/child-\d{3})\/evidence\/diagnostics\/[A-Za-z0-9._-]+\/([^/]+)$/.exec(name);
  if (!match || !APPROVED_DIAGNOSTIC_FILES.includes(match[1])) return false;
  try {
    validateRetainedDiagnostic(match[1], readFileSync(path));
    return true;
  } catch {
    return false;
  }
}

function isValidTest(test) {
  return test
    && typeof test.id === 'string'
    && test.id.length > 0
    && (test.journey === null || typeof test.journey === 'string')
    && CHILD_STATUSES.has(test.status)
    && Number.isFinite(test.durationMs)
    && test.durationMs >= 0
    && Number.isInteger(test.retry)
    && test.retry >= 0;
}

function countsMatch(counts, tests) {
  if (!counts || typeof counts !== 'object' || Array.isArray(counts)) return false;
  const actual = tests.reduce((result, test) => ({
    ...result,
    [test.status]: (result[test.status] ?? 0) + 1,
  }), {});
  return JSON.stringify(counts) === JSON.stringify(actual);
}

function readJson(path, error, errors) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    errors.push(error);
    return null;
  }
}
