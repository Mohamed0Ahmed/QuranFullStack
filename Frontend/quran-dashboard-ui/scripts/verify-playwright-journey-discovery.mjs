import assert from 'node:assert/strict';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

import CriticalJourneyDiscoveryReporter from './discover-playwright-journeys.mjs';

const frontendRoot = fileURLToPath(new URL('..', import.meta.url));
const e2eRoot = join(frontendRoot, 'e2e');

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
  annotation('read-only'),
  annotation('artifact', 'compact-cross-stack-base'),
  annotation('journey', 'quran-fidelity.reader'),
];

verifySuccess('selects a complete critical read-only journey', [requiredReadOnly], [
  {
    artifact: 'compact-cross-stack-base',
    file: 'contract.e2e.ts',
    journey: 'quran-fidelity.reader',
    line: 4,
    mobile: false,
    project: 'default',
    state: 'read-only',
    title: 'contract journey 1',
  },
]);

verifySuccess(
  'accepts the optional mobile annotation',
  [[...requiredReadOnly, annotation('mobile')]],
  [
    {
      artifact: 'compact-cross-stack-base',
      file: 'contract.e2e.ts',
      journey: 'quran-fidelity.reader',
      line: 4,
      mobile: true,
      project: 'default',
      state: 'read-only',
      title: 'contract journey 1',
    },
  ],
);

verifyFailure(
  'rejects missing required metadata',
  [[annotation('critical'), annotation('read-only'), annotation('journey', 'quran-fidelity.reader')]],
  /missing required artifact annotation/i,
);

verifyFailure(
  'rejects contradictory state metadata',
  [[...requiredReadOnly, annotation('mutating')]],
  /contradictory.*mutating.*read-only/i,
);

verifyFailure(
  'rejects duplicated metadata on one journey',
  [[...requiredReadOnly, annotation('artifact', 'another-artifact')]],
  /duplicate artifact annotations/i,
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

verifyFailure('rejects an empty critical selection', [[]], /no critical journeys were discovered/i);

console.log('Playwright journey discovery contract passed.');
