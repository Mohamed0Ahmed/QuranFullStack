import { readFileSync } from 'node:fs';
import { isAbsolute, relative, resolve } from 'node:path';

const PHASES = new Set([
  'preparation',
  'browser-provisioning',
  'browser-execution',
  'artifact-verification',
  'operational-risk',
]);
const RESULTS_PLACEHOLDER = '{NIGHTLY_RESULTS_DIR}';
const PROHIBITED_COMMAND_TEXT = [
  'dependency-advisory',
  'npm audit',
  'dotnet list package',
  'run-dependency-advisory-evaluation',
];
const REQUIRED_BROWSER_JOURNEYS = [
  'quran-fidelity.mushaf-mobile',
  'linking.successful-owner-mobile',
];
const REQUIRED_COMMANDS = new Map([
  ['sealed-browser-provisioning', { executable: 'npm', arguments: ['run', 'e2e:provision'] }],
  ['full-chromium-suite', { executable: 'npm', arguments: ['run', 'e2e'] }],
  ['verify-full-canonical-artifact', { executable: 'Backend/scripts/test-artifacts', arguments: ['verify-content-addressed', '--artifact', 'quran-canonical'] }],
  ['phrase-index-build-activation', { executable: 'Backend/scripts/test-backend', arguments: ['phrase-index-rehearsal', '--no-build', '--results-dir', '{NIGHTLY_RESULTS_DIR}/phrase-index-rehearsal'] }],
  ['abwab-snapshot-protections', { executable: 'Backend/scripts/test-backend', arguments: ['feature', '--class', 'QuranDashboard.Tests.Abwab.AbwabSnapshotWorkflowTests', '--no-build', '--results-dir', '{NIGHTLY_RESULTS_DIR}/abwab-snapshot'] }],
  ['quran-topics-import-protections', { executable: 'Backend/scripts/test-backend', arguments: ['feature', '--class', 'QuranDashboard.Tests.Quran.QuranTopicsBook.QuranTopicsBookImportTests', '--no-build', '--results-dir', '{NIGHTLY_RESULTS_DIR}/quran-topics'] }],
]);

export function loadNightlyRiskManifest(path, repositoryRoot) {
  let manifest;
  try {
    manifest = JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read nightly risk manifest: ${error.message}`);
  }
  validateNightlyRiskManifest(manifest, repositoryRoot);
  return manifest;
}

export function validateNightlyRiskManifest(manifest, repositoryRoot) {
  requireCondition(manifest?.schemaVersion === 1, 'schemaVersion must be 1.');
  requireNonEmptyString(manifest.id, 'id');
  requireNonEmptyString(manifest.title, 'title');
  requireCondition(manifest.providerNeutral === true, 'providerNeutral must be true.');
  requirePositiveInteger(manifest.timeoutSeconds, 'timeoutSeconds');
  requireCondition(manifest.inputContract && typeof manifest.inputContract === 'object', 'inputContract is required.');
  for (const input of ['artifactRoot', 'resultsDirectory', 'executionKind']) {
    requireNonEmptyString(manifest.inputContract[input], `inputContract.${input}`);
  }
  requireCondition(
    manifest.evidenceRetention?.externalUploadConfigured === false,
    'external artifact upload must remain unconfigured.',
  );
  requirePositiveInteger(manifest.evidenceRetention?.failedDiagnosticsDays, 'failedDiagnosticsDays');
  requirePositiveInteger(manifest.evidenceRetention?.aggregateTimingDays, 'aggregateTimingDays');
  requireCondition(
    Array.isArray(manifest.excludedWork)
      && manifest.excludedWork.includes('dependency-advisory-evaluation'),
    'dependency advisory evaluation must be explicitly excluded.',
  );
  requireCondition(
    Array.isArray(manifest.requiredBrowserJourneys)
      && manifest.requiredBrowserJourneys.length > 0
      && manifest.requiredBrowserJourneys.every((journey) => typeof journey === 'string' && journey.length > 0),
    'requiredBrowserJourneys must be a non-empty array of identifiers.',
  );
  requireCondition(Array.isArray(manifest.commands) && manifest.commands.length > 0, 'commands must be non-empty.');

  const commandIds = new Set();
  let diagnosticRetryCommands = 0;
  for (const command of manifest.commands) {
    requireNonEmptyString(command.id, 'command.id');
    requireCondition(!commandIds.has(command.id), `duplicate command id: ${command.id}.`);
    commandIds.add(command.id);
    requireCondition(PHASES.has(command.phase), `${command.id}.phase is unsupported.`);
    requirePositiveInteger(command.timeoutSeconds, `${command.id}.timeoutSeconds`);
    requireNonEmptyString(command.cwd, `${command.id}.cwd`);
    requireCondition(
      isInside(repositoryRoot, resolve(repositoryRoot, command.cwd)),
      `${command.id}.cwd must stay inside the repository.`,
    );
    requireNonEmptyString(command.executable, `${command.id}.executable`);
    requireCondition(
      Array.isArray(command.arguments) && command.arguments.every((argument) => typeof argument === 'string'),
      `${command.id}.arguments must be an array of strings.`,
    );
    const commandText = [command.id, command.executable, ...command.arguments].join(' ').toLowerCase();
    requireCondition(
      !PROHIBITED_COMMAND_TEXT.some((term) => commandText.includes(term)),
      `${command.id} must not invoke dependency advisory evaluation.`,
    );
    if (command.diagnosticRetry === true) diagnosticRetryCommands += 1;
    else requireCondition(command.diagnosticRetry === undefined, `${command.id}.diagnosticRetry must be true when present.`);
    if (manifest.id === 'nightly-risk') {
      requireCondition(command.runtimeCleanupScript === undefined, `${command.id}.runtimeCleanupScript is fixture-only.`);
    }
  }
  for (const command of manifest.commands) {
    requireCondition(
      command.dependsOn === undefined
        || (Array.isArray(command.dependsOn)
          && command.dependsOn.every((dependency) => typeof dependency === 'string' && commandIds.has(dependency) && dependency !== command.id)),
      `${command.id}.dependsOn must contain known non-self command IDs.`,
    );
  }
  requireCondition(diagnosticRetryCommands <= 1, 'at most one command may allow a diagnostic retry.');
  if (manifest.id === 'nightly-risk') validateRequiredNightlyCommands(manifest, commandIds);
}

function validateRequiredNightlyCommands(manifest, commandIds) {
  requireCondition(
    commandIds.size === REQUIRED_COMMANDS.size
      && [...REQUIRED_COMMANDS.keys()].every((id) => commandIds.has(id)),
    'the nightly lane must contain exactly the approved command set.',
  );
  for (const [id, expected] of REQUIRED_COMMANDS) {
    const command = manifest.commands.find((candidate) => candidate.id === id);
    requireCondition(
      command.executable === expected.executable
        && JSON.stringify(command.arguments) === JSON.stringify(expected.arguments),
      `${id} must use its approved command contract.`,
    );
  }
  const browser = manifest.commands.find((command) => command.id === 'full-chromium-suite');
  requireCondition(
    browser.approvedReporterArtifacts === true,
    'the full Chromium suite must require approved reporter artifacts.',
  );
  requireCondition(browser.diagnosticRetry === true, 'only the full Chromium suite may allow a diagnostic retry.');
  requireCondition(browser.runtimeCleanup === true, 'the full Chromium suite must declare owned-runtime cleanup.');
  requireCondition(
    JSON.stringify(browser.dependsOn) === JSON.stringify(['sealed-browser-provisioning']),
    'the full Chromium suite must depend on sealed provisioning.',
  );
  const provisioning = manifest.commands.find((command) => command.id === 'sealed-browser-provisioning');
  requireCondition(
    JSON.stringify(provisioning.provides) === JSON.stringify(['backend-build']),
    'sealed provisioning must be the only Backend build owner.',
  );
  for (const command of manifest.commands.filter((command) => command.phase === 'operational-risk')) {
    requireCondition(
      command.testEvidence?.lane === command.arguments[0]
        && typeof command.testEvidence.class === 'string'
        && command.testEvidence.class.startsWith('QuranDashboard.Tests.'),
      `${command.id} must declare its expected lane and test class.`,
    );
    requireCondition(
      Array.isArray(command.dependsOn) && command.dependsOn.includes('sealed-browser-provisioning'),
      `${command.id} must depend on sealed provisioning.`,
    );
    requireCondition(command.databaseOwning === true, `${command.id} must declare database ownership.`);
  }
  requireCondition(
    manifest.commands.filter((command) => command.diagnosticRetry === true).length === 1,
    'the full Chromium diagnostic retry must be unique.',
  );
  requireCondition(
    manifest.commands.reduce((total, command) => total + command.timeoutSeconds, 0) + 60
      <= manifest.timeoutSeconds,
    'the outer deadline must cover all primary timeouts and the browser cleanup allowance.',
  );
  requireCondition(
    JSON.stringify(manifest.requiredBrowserJourneys) === JSON.stringify(REQUIRED_BROWSER_JOURNEYS),
    'the nightly lane must require exactly the approved mobile journeys.',
  );
}

export function materializeNightlyCommand(command, repositoryRoot, resultsDirectory) {
  return {
    ...command,
    cwd: resolve(repositoryRoot, command.cwd),
    arguments: command.arguments.map((argument) => argument.replaceAll(RESULTS_PLACEHOLDER, resultsDirectory)),
  };
}

function isInside(parent, child) {
  const pathFromParent = relative(resolve(parent), resolve(child));
  return pathFromParent === '' || (!pathFromParent.startsWith('..') && !isAbsolute(pathFromParent));
}

function requirePositiveInteger(value, name) {
  requireCondition(Number.isInteger(value) && value > 0, `${name} must be a positive integer.`);
}

function requireNonEmptyString(value, name) {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string.`);
}

function requireCondition(condition, message) {
  if (!condition) throw new Error(`Invalid nightly risk manifest: ${message}`);
}
