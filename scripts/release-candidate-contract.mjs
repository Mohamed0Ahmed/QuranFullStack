import { readFileSync } from 'node:fs';
import { isAbsolute, relative, resolve, sep } from 'node:path';

const RELEASE_RESULTS_PLACEHOLDER = '{RELEASE_RESULTS_DIR}';
const REQUIRED_COMMANDS = new Map([
  ['locked-backend-restore', ['dotnet', ['restore', 'Backend/QuranDashboard.sln', '--locked-mode', '--disable-parallel', '-m:1', '-p:BuildInParallel=false', '-p:RestoreDisableParallel=true']]],
  ['no-restore-backend-build', ['dotnet', ['build', 'Backend/QuranDashboard.sln', '--no-restore', '-m:1', '-p:BuildInParallel=false']]],
  ['inspect-test-runtime', ['dotnet', ['Backend/tools/QuranDashboard.TestRuntime/bin/Debug/net10.0/QuranDashboard.TestRuntime.dll', 'inspect']]],
  ['release-dependency-advisory', ['node', ['scripts/run-dependency-advisory-evaluation.mjs', '--trigger', 'release', '--results-dir', '{RELEASE_RESULTS_DIR}/dependency-advisory']]],
]);

export function loadReleaseCandidateManifest(path, repositoryRoot) {
  let manifest;
  try {
    manifest = JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read release candidate manifest: ${error.message}`);
  }
  validateReleaseCandidateManifest(manifest, repositoryRoot);
  return manifest;
}

export function validateReleaseCandidateManifest(manifest, repositoryRoot) {
  requireCondition(manifest?.schemaVersion === 2, 'schemaVersion must be 2.');
  requireCondition(manifest.id === 'release-candidate', 'id must be release-candidate.');
  requireNonEmptyString(manifest.title, 'title');
  requireCondition(manifest.executionScope === 'local-first-pre-merge', 'executionScope must be local-first-pre-merge.');
  requireCondition(manifest.providerNeutral === true, 'providerNeutral must be true.');
  requirePositiveInteger(manifest.timeoutSeconds, 'timeoutSeconds');
  requireCondition(manifest.artifact === undefined, 'artifact pins are not part of Local-first pre-merge verification.');
  requireCondition(manifest.previousRelease === undefined, 'previous-release dump rehearsals are not part of Local-first pre-merge verification.');
  requireCondition(manifest.externalEvidence === undefined, 'external evidence is not part of Local-first pre-merge verification.');
  requireCondition(Array.isArray(manifest.commands) && manifest.commands.length === REQUIRED_COMMANDS.size, 'commands are incomplete.');

  const ids = new Set();
  for (const command of manifest.commands) {
    requireNonEmptyString(command.id, 'command.id');
    requireCondition(!ids.has(command.id), `duplicate command id: ${command.id}.`);
    ids.add(command.id);
    requirePositiveInteger(command.timeoutSeconds, `${command.id}.timeoutSeconds`);
    requireNonEmptyString(command.cwd, `${command.id}.cwd`);
    requireCondition(isInside(repositoryRoot, resolve(repositoryRoot, command.cwd)), `${command.id}.cwd must stay inside the repository.`);
    requireNonEmptyString(command.executable, `${command.id}.executable`);
    requireCondition(Array.isArray(command.arguments) && command.arguments.every((argument) => typeof argument === 'string'), `${command.id}.arguments must be strings.`);
    const expected = REQUIRED_COMMANDS.get(command.id);
    requireCondition(expected !== undefined, `unexpected command: ${command.id}.`);
    requireCondition(command.executable === expected[0] && JSON.stringify(command.arguments) === JSON.stringify(expected[1]), `${command.id} does not use its existing executable seam.`);
  }
  for (const command of manifest.commands) {
    requireCondition(
      command.dependsOn === undefined || (Array.isArray(command.dependsOn) && command.dependsOn.every((dependency) => ids.has(dependency) && dependency !== command.id)),
      `${command.id}.dependsOn is invalid.`,
    );
  }
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'no-restore-backend-build').dependsOn) === JSON.stringify(['locked-backend-restore']),
    'the no-restore build must depend on locked restore.',
  );
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'inspect-test-runtime').dependsOn) === JSON.stringify(['no-restore-backend-build']),
    'TestRuntime inspect must depend on the controlled build.',
  );
}

export function materializeReleaseCandidateCommand(command, repositoryRoot, resultsDirectory) {
  return {
    ...command,
    cwd: resolve(repositoryRoot, command.cwd),
    arguments: command.arguments.map((argument) => argument.replaceAll(RELEASE_RESULTS_PLACEHOLDER, resultsDirectory)),
  };
}

function isExactObject(value, keys) {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
    && JSON.stringify(Object.keys(value).sort()) === JSON.stringify([...keys].sort());
}

export function validatePrimaryEvidence(command, resultsDirectory) {
  if (command === 'release-dependency-advisory') return validateAdvisoryEvidence(resultsDirectory);
  return { status: 'passed', checkId: 'no-primary-evidence-required' };
}

function validateAdvisoryEvidence(resultsDirectory) {
  const evaluation = readJsonOrNull(resolve(resultsDirectory, 'evaluation.json'));
  return isExactObject(evaluation, ['schemaVersion', 'policyId', 'trigger', 'evaluatedAt', 'status', 'summary', 'findings', 'blockingFindings', 'expiredWaivers', 'scanErrors'])
    && evaluation.schemaVersion === 1 && evaluation.policyId === 'dependency-advisory-evaluation' && evaluation.trigger === 'release'
    && ['passed', 'passed-with-notes'].includes(evaluation.status) && isExactObject(evaluation.summary, ['total', 'production', 'development', 'highCriticalProduction', 'blocking'])
    && Object.values(evaluation.summary).every((count) => Number.isInteger(count) && count >= 0)
    && Array.isArray(evaluation.findings) && evaluation.summary.total === evaluation.findings.length && evaluation.summary.blocking === 0
    && Array.isArray(evaluation.blockingFindings) && evaluation.blockingFindings.length === 0
    && Array.isArray(evaluation.expiredWaivers) && evaluation.expiredWaivers.length === 0
    && evaluation.scanErrors === null
    ? { status: 'passed', checkId: 'release-advisory-evaluation-validated' }
    : { status: 'failed', checkId: 'release-advisory-evaluation-missing-or-invalid' };
}

function readJsonOrNull(path) {
  try { return JSON.parse(readFileSync(path, 'utf8')); } catch { return null; }
}

function isInside(parent, child) {
  const pathFromParent = relative(resolve(parent), resolve(child));
  return pathFromParent === ''
    || (pathFromParent !== '..' && !pathFromParent.startsWith(`..${sep}`) && !isAbsolute(pathFromParent));
}

function requirePositiveInteger(value, name) {
  requireCondition(Number.isInteger(value) && value > 0, `${name} must be a positive integer.`);
}

function requireNonEmptyString(value, name) {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string.`);
}

function requireCondition(condition, message) {
  if (!condition) throw new Error(`Invalid release candidate manifest: ${message}`);
}
