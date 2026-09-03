import assert from 'node:assert/strict';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

import CriticalJourneyDiscoveryReporter from './discover-playwright-journeys.mjs';

const frontendRoot = fileURLToPath(new URL('..', import.meta.url));
const e2eRoot = join(frontendRoot, 'e2e');
const policyContract = {
  schemaVersion: 1,
  fixtureProfiles: {
    canonical: {
      setupWrites: [],
      resetBehavior: 'none',
      databaseTarget: 'test-database',
      startupEffects: ['read-only-api'],
    },
    mutating: {
      setupWrites: ['mutable-application-state'],
      resetBehavior: 'mutable-application-state',
      databaseTarget: 'test-database',
      startupEffects: ['mutable-api'],
    },
  },
  migrationInventory: [],
};

function annotation(type, description) {
  return description === undefined ? { type } : { type, description };
}

function discoverySuite(annotationGroups) {
  return {
    allTests: () =>
      annotationGroups.map((annotations, index) => ({
        annotations,
        location: {
          file: join(e2eRoot, 'contract.e2e.ts'),
          line: index + 4,
        },
        parent: { project: () => ({ name: 'default' }) },
        title: `contract journey ${index + 1}`,
      })),
  };
}

function discover(annotationGroups) {
  let stdout = '';
  let stderr = '';
  const reporter = new CriticalJourneyDiscoveryReporter({
    policyContract,
    stdout: { write: (value) => (stdout += value) },
    stderr: { write: (value) => (stderr += value) },
  });

  reporter.onBegin({ rootDir: e2eRoot }, discoverySuite(annotationGroups));
  const result = reporter.onEnd();

  return { result, stderr, stdout };
}

function verifySuccess(name, annotationGroups, expected) {
  const discovery = discover(annotationGroups);

  assert.equal(discovery.result, undefined, `${name}\n${discovery.stderr}`);
  assert.deepEqual(JSON.parse(discovery.stdout), expected, name);
}

function verifyFailure(name, annotationGroups, expectedMessage) {
  const discovery = discover(annotationGroups);

  assert.deepEqual(discovery.result, { status: 'failed' }, name);
  assert.match(discovery.stderr, expectedMessage, name);
  assert.equal(discovery.stdout, '', name);
}

const requiredReadOnly = [
  annotation('critical'),
  annotation('canonical-read'),
  annotation('fixture-policy', 'canonical'),
  annotation('journey', 'quran-fidelity.reader'),
];

verifySuccess('selects a complete critical read-only journey', [requiredReadOnly], [
  {
    file: 'contract.e2e.ts',
    fixtureProfile: 'canonical',
    journey: 'quran-fidelity.reader',
    line: 4,
    mobile: false,
    project: 'default',
    state: 'canonical-read',
    title: 'contract journey 1',
  },
]);

verifySuccess(
  'accepts the optional mobile annotation',
  [[...requiredReadOnly, annotation('mobile')]],
  [
    {
      file: 'contract.e2e.ts',
      fixtureProfile: 'canonical',
      journey: 'quran-fidelity.reader',
      line: 4,
      mobile: true,
      project: 'default',
      state: 'canonical-read',
      title: 'contract journey 1',
    },
  ],
);

verifyFailure(
  'rejects missing required metadata',
  [[annotation('critical'), annotation('canonical-read'), annotation('journey', 'quran-fidelity.reader')]],
  /exactly one fixture-policy annotation/i,
);

verifyFailure(
  'rejects contradictory state metadata',
  [[...requiredReadOnly, annotation('guarded-read')]],
  /exactly one state policy/i,
);

verifyFailure(
  'rejects duplicated metadata on one journey',
  [[...requiredReadOnly, annotation('fixture-policy', 'mutating')]],
  /exactly one fixture-policy annotation/i,
);

verifyFailure(
  'rejects a duplicated journey identifier',
  [requiredReadOnly, requiredReadOnly],
  /duplicate journey identifier.*quran-fidelity\.reader/i,
);

verifyFailure(
  'rejects unsupported journey metadata',
  [[...requiredReadOnly, annotation('desktop')]],
  /unsupported annotation.*desktop/i,
);

verifyFailure(
  'rejects an empty critical selection',
  [[annotation('canonical-read'), annotation('fixture-policy', 'canonical')]],
  /no critical journeys were discovered/i,
);

console.log('Playwright journey discovery contract passed.');
