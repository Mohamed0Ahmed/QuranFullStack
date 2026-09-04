const STATEFUL_POLICIES = new Set(['guarded-read', 'mutating']);
const EXACT_SELECTOR = /^e2e\/[a-z0-9][a-z0-9._-]*\.e2e\.ts:[1-9][0-9]*$/;

export const STATEFUL_LOCK_COMMAND = 'playwright-stateful';
export const STATEFUL_API_PORT = 5015;

export function classifyInteractivePlaywrightSelector(discovered, requestedMode, selector) {
  if (!Array.isArray(discovered)) {
    throw new Error('Playwright policy discovery returned an invalid result.');
  }
  if (!['read-only', 'mutating'].includes(requestedMode)) {
    throw new Error('Interactive Playwright requires read-only or mutating mode.');
  }
  if (!EXACT_SELECTOR.test(selector ?? '')) {
    throw new Error(`Invalid Playwright selector: ${selector ?? ''}`);
  }
  const matches = discovered.filter((test) => `${test.file}:${test.line}` === selector);
  if (matches.length === 0) {
    throw new Error(`Unknown Playwright selector: ${selector}`);
  }
  const policies = new Set(matches.map((test) => test.declaredPolicy));
  if (policies.size !== 1) {
    throw new Error(`Playwright selector has contradictory policies: ${selector}`);
  }
  const [policy] = policies;
  const matchesMode = requestedMode === 'mutating'
    ? policy === 'mutating'
    : policy === 'canonical-read' || policy === 'guarded-read';
  if (!matchesMode) {
    throw new Error(
      `Playwright selector ${selector} policy ${policy} does not match ${requestedMode} mode.`,
    );
  }
  return policy;
}

export function buildMutableResetArguments({ apiProcessId, expectedFingerprint, phase, runId }) {
  if (!/^[A-Za-z0-9._-]{1,32}$/.test(runId ?? '')) {
    throw new Error('Mutable Playwright reset requires a valid run ID.');
  }
  if (!/^[a-f0-9]{64}$/i.test(expectedFingerprint ?? '')) {
    throw new Error('Mutable Playwright reset requires a Protected State fingerprint.');
  }
  if (!['initial', 'final'].includes(phase)) {
    throw new Error('Mutable Playwright reset requires an initial or final phase.');
  }
  if (phase === 'final' && !Number.isInteger(apiProcessId)) {
    throw new Error('Final mutable Playwright reset requires a verified API process ID.');
  }
  if (apiProcessId !== null && (!Number.isInteger(apiProcessId) || apiProcessId < 1)) {
    throw new Error('Mutable Playwright reset received an invalid API process ID.');
  }

  return [
    'reset',
    '--run-id',
    runId,
    '--command',
    STATEFUL_LOCK_COMMAND,
    '--expected-fingerprint',
    expectedFingerprint,
    '--api-port',
    String(STATEFUL_API_PORT),
    '--api-process-id',
    apiProcessId === null ? 'none' : String(apiProcessId),
    '--phase',
    phase,
  ];
}

export function validateApiProcessReceipt(receipt, expectedPort = STATEFUL_API_PORT) {
  if (
    !receipt
    || receipt.schemaVersion !== 1
    || !Number.isInteger(receipt.processId)
    || receipt.processId < 1
  ) {
    throw new Error('Stateful Playwright final cleanup has an unverified API process identity.');
  }
  if (receipt.port !== expectedPort) {
    throw new Error('Stateful Playwright API process receipt does not match the expected port.');
  }
  return receipt.processId;
}

export function selectStatefulPlaywrightTests(discovered, focusedSelector) {
  if (!Array.isArray(discovered)) {
    throw new Error('Playwright policy discovery returned an invalid result.');
  }

  const scenarios = statefulScenarios(discovered);
  if (focusedSelector === undefined) {
    return scenarios;
  }
  if (!EXACT_SELECTOR.test(focusedSelector)) {
    throw new Error(`Invalid Playwright selector: ${focusedSelector}`);
  }

  const matches = discovered.filter((test) => `${test.file}:${test.line}` === focusedSelector);
  if (matches.length === 0) {
    throw new Error(`Unknown Playwright selector: ${focusedSelector}`);
  }
  if (matches.some((test) => !STATEFUL_POLICIES.has(test.declaredPolicy))) {
    throw new Error(`Playwright selector ${focusedSelector} is not stateful.`);
  }

  return statefulScenarios(matches);
}

export function selectStatefulCriticalJourneys(journeys) {
  if (!Array.isArray(journeys)) {
    throw new Error('Critical Playwright discovery returned an invalid result.');
  }

  return statefulScenarios(
    journeys
      .filter((journey) => STATEFUL_POLICIES.has(journey.state))
      .map((journey) => ({
        ...journey,
        declaredPolicy: journey.state,
        file: `e2e/${journey.file}`,
      })),
  );
}

export function buildStatefulPlaywrightEnvironment(source, scenario, paths) {
  const connectionString = source.ConnectionStrings__QuranDashboardTest?.trim();
  if (!connectionString) {
    throw new Error(
      'Stateful Playwright execution requires ConnectionStrings__QuranDashboardTest.',
    );
  }
  validateScenario(scenario);
  requirePath(paths.apiProcessReceipt, 'API process receipt');
  requirePath(paths.backendAssembly, 'Backend assembly');
  requirePath(paths.evidenceDirectory, 'evidence directory');
  requirePath(paths.playwrightOutputDirectory, 'Playwright output directory');
  if (!/^[A-Za-z0-9._-]{1,32}$/.test(paths.runId ?? '')) {
    throw new Error('Stateful Playwright execution requires a valid run ID.');
  }

  const environment = { ...source };
  for (const name of Object.keys(environment)) {
    if (
      name === 'E2E_ARTIFACT_VERIFIER_ASSEMBLY'
      || name === 'E2E_BACKEND_ASSEMBLY'
      || name === 'E2E_ORCHESTRATOR_GUARDED'
      || name === 'E2E_PREPARED_DATABASE'
      || name === 'E2E_SEALED_EXECUTION'
      || name === 'QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT'
      || name === 'QURAN_DASHBOARD_TEST_RUNTIME_WRITER_CONTEXT'
      || name === 'QURAN_TEST_ARTIFACT_ROOT'
      || name.startsWith('Testing__DatabaseActivity__EnabledBackgroundActivities__')
    ) {
      delete environment[name];
    }
  }

  const readOnly = scenario.policy === 'guarded-read';
  const configured = {
    ...environment,
    ConnectionStrings__QuranDashboardTest: connectionString,
    E2E_API_PROCESS_RECEIPT: paths.apiProcessReceipt,
    E2E_BACKEND_ASSEMBLY: paths.backendAssembly,
    E2E_DATABASE_MODE: 'persistent-stateful',
    E2E_EVIDENCE_DIRECTORY: paths.evidenceDirectory,
    E2E_PLAYWRIGHT_OUTPUT_DIRECTORY: paths.playwrightOutputDirectory,
    E2E_PLAYWRIGHT_POLICY_PARTITION: scenario.policy,
    QURAN_DASHBOARD_TEST_LOCK_COMMAND: STATEFUL_LOCK_COMMAND,
    QURAN_DASHBOARD_TEST_RUN_ID: paths.runId,
    Testing__DatabaseActivity__Profile: readOnly ? 'ReadOnly' : 'Mutable',
    ...(readOnly
      ? { QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT: 'verified-v1' }
      : { QURAN_DASHBOARD_TEST_RUNTIME_WRITER_CONTEXT: 'verified-v1' }),
  };
  scenario.backgroundActivities.forEach((activity, index) => {
    configured[`Testing__DatabaseActivity__EnabledBackgroundActivities__${index}`] = activity;
  });
  return configured;
}

function statefulScenarios(entries) {
  const scenarios = new Map();
  for (const entry of entries) {
    if (!STATEFUL_POLICIES.has(entry.declaredPolicy)) continue;
    const scenario = {
      backgroundActivities: [...(entry.backgroundActivities ?? [])],
      fixtureProfile: entry.fixtureProfile,
      policy: entry.declaredPolicy,
      selector: `${entry.file}:${entry.line}`,
    };
    validateScenario(scenario);
    const serialized = JSON.stringify(scenario);
    const previous = scenarios.get(scenario.selector);
    if (previous && previous !== serialized) {
      throw new Error(`Playwright selector has contradictory stateful policies: ${scenario.selector}`);
    }
    scenarios.set(scenario.selector, serialized);
  }
  return [...scenarios.values()].map((scenario) => JSON.parse(scenario)).sort((left, right) =>
    left.selector.localeCompare(right.selector),
  );
}

function validateScenario(scenario) {
  if (!STATEFUL_POLICIES.has(scenario?.policy)) {
    throw new Error('Stateful Playwright scenario has an unsupported policy.');
  }
  if (!EXACT_SELECTOR.test(scenario.selector ?? '')) {
    throw new Error(`Invalid Playwright selector: ${scenario?.selector ?? ''}`);
  }
  if (typeof scenario.fixtureProfile !== 'string' || scenario.fixtureProfile.length === 0) {
    throw new Error('Stateful Playwright scenario requires a fixture profile.');
  }
  if (!Array.isArray(scenario.backgroundActivities)) {
    throw new Error('Stateful Playwright scenario requires background activity metadata.');
  }
  if (scenario.policy === 'guarded-read' && scenario.backgroundActivities.length > 0) {
    throw new Error('Guarded-read Playwright cannot enable background activity.');
  }
}

function requirePath(value, label) {
  if (typeof value !== 'string' || !value.startsWith('/')) {
    throw new Error(`Stateful Playwright execution requires an absolute ${label} path.`);
  }
}
