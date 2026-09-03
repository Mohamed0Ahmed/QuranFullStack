const CANONICAL_GROUP = 'CanonicalReader';
const EXACT_SELECTOR = /^e2e\/[a-z0-9][a-z0-9._-]*\.e2e\.ts:[1-9][0-9]*$/;

export function buildCanonicalPlaywrightEnvironment(source) {
  const connectionString = source.ConnectionStrings__QuranDashboardTest?.trim();
  if (!connectionString) {
    throw new Error(
      'Canonical Playwright execution requires ConnectionStrings__QuranDashboardTest.',
    );
  }

  const environment = { ...source };
  for (const name of Object.keys(environment)) {
    if (
      name === 'E2E_ARTIFACT_VERIFIER_ASSEMBLY'
      || name === 'E2E_ORCHESTRATOR_GUARDED'
      || name === 'E2E_PREPARED_DATABASE'
      || name === 'E2E_SEALED_EXECUTION'
      || name === 'QURAN_TEST_ARTIFACT_ROOT'
      || name.startsWith('Testing__DatabaseActivity__EnabledBackgroundActivities__')
    ) {
      delete environment[name];
    }
  }

  return {
    ...environment,
    ConnectionStrings__QuranDashboardTest: connectionString,
    E2E_DATABASE_MODE: 'persistent-read-only',
    E2E_PLAYWRIGHT_POLICY_PARTITION: 'canonical-read',
    QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT: 'verified-v1',
    Testing__DatabaseActivity__Profile: 'ReadOnly',
  };
}

export function selectCanonicalPlaywrightTests(discovered, focusedSelector) {
  if (!Array.isArray(discovered)) {
    throw new Error('Playwright policy discovery returned an invalid result.');
  }

  const canonicalSelectors = discovered
    .filter((test) => test.effectiveGroup === CANONICAL_GROUP)
    .map((test) => `${test.file}:${test.line}`);

  if (focusedSelector === undefined) {
    return [...new Set(canonicalSelectors)].sort();
  }
  if (!EXACT_SELECTOR.test(focusedSelector)) {
    throw new Error(`Invalid Playwright selector: ${focusedSelector}`);
  }

  const matches = discovered.filter(
    (test) => `${test.file}:${test.line}` === focusedSelector,
  );
  if (matches.length === 0) {
    throw new Error(`Unknown Playwright selector: ${focusedSelector}`);
  }
  if (matches.some((test) => test.effectiveGroup !== CANONICAL_GROUP)) {
    throw new Error(`Playwright selector ${focusedSelector} is not canonical-read.`);
  }

  return [focusedSelector];
}

export function selectNonCanonicalPlaywrightTests(discovered) {
  if (!Array.isArray(discovered)) {
    throw new Error('Playwright policy discovery returned an invalid result.');
  }

  return [
    ...new Set(
      discovered
        .filter((test) => test.effectiveGroup !== CANONICAL_GROUP)
        .map((test) => `${test.file}:${test.line}`),
    ),
  ].sort();
}

export function classifyFocusedPlaywrightSelector(discovered, selector) {
  if (!EXACT_SELECTOR.test(selector)) {
    throw new Error(`Invalid Playwright selector: ${selector}`);
  }
  const matches = discovered.filter((test) => `${test.file}:${test.line}` === selector);
  if (matches.length === 0) {
    throw new Error(`Unknown Playwright selector: ${selector}`);
  }
  const groups = new Set(matches.map((test) => test.effectiveGroup));
  if (groups.size !== 1) {
    throw new Error(`Playwright selector has contradictory project policies: ${selector}`);
  }
  return groups.has(CANONICAL_GROUP) ? 'canonical-read' : 'non-canonical';
}

export function selectCanonicalCriticalJourneys(journeys) {
  if (!Array.isArray(journeys)) {
    throw new Error('Critical Playwright discovery returned an invalid result.');
  }

  return [
    ...new Set(
      journeys
        .filter((journey) => journey.state === 'canonical-read')
        .map((journey) => `e2e/${journey.file}:${journey.line}`),
    ),
  ].sort();
}
