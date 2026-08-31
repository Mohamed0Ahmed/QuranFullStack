import { readFileSync } from 'node:fs';
import { isAbsolute, relative, resolve } from 'node:path';

const ALLOWED_PHASES = new Set(['provisioning', 'execution']);
const RESULTS_PLACEHOLDER = '{JOB_RESULTS_DIR}';

export function loadObservationMatrix(path, repositoryRoot) {
  let matrix;
  try {
    matrix = JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read observation matrix ${path}: ${error.message}`);
  }

  validateObservationMatrix(matrix, repositoryRoot);
  return matrix;
}

export function validateObservationMatrix(matrix, repositoryRoot) {
  requireCondition(matrix?.schemaVersion === 1, 'schemaVersion must be 1.');
  requireNonEmptyString(matrix.id, 'id');
  requireCondition(matrix.scheduling === 'parallel', 'scheduling must be parallel.');
  requireNonEmptyString(matrix.durationScope, 'durationScope');
  requireCondition(
    Array.isArray(matrix.durationComponents)
      && matrix.durationComponents.length > 0
      && matrix.durationComponents.every(
        (component) => typeof component === 'string' && component.length > 0,
      ),
    'durationComponents must be a non-empty array of strings.',
  );
  requireCondition(Array.isArray(matrix.jobs) && matrix.jobs.length > 0, 'jobs must be non-empty.');

  const jobIds = new Set();
  for (const job of matrix.jobs) {
    requireNonEmptyString(job.id, 'job.id');
    requireCondition(!jobIds.has(job.id), `Duplicate job id: ${job.id}.`);
    jobIds.add(job.id);
    requireNonEmptyString(job.title, `${job.id}.title`);
    requireCondition(typeof job.policy?.blocking === 'boolean', `${job.id}.policy.blocking is required.`);
    requireCondition(job.policy?.maxAttempts === 1, `${job.id}.policy.maxAttempts must be 1.`);
    requireCondition(
      Number.isInteger(job.policy?.timeoutSeconds) && job.policy.timeoutSeconds > 0,
      `${job.id}.policy.timeoutSeconds must be a positive integer.`,
    );
    requireCondition(
      job.policy?.queueTimeIncluded === false,
      `${job.id}.policy.queueTimeIncluded must be false.`,
    );
    requireCondition(
      Array.isArray(job.commands) && job.commands.length > 0,
      `${job.id}.commands must be non-empty.`,
    );

    const commandIds = new Set();
    for (const command of job.commands) {
      requireNonEmptyString(command.id, `${job.id}.command.id`);
      requireCondition(
        !commandIds.has(command.id),
        `Duplicate command id ${command.id} in job ${job.id}.`,
      );
      commandIds.add(command.id);
      requireCondition(
        ALLOWED_PHASES.has(command.phase),
        `${job.id}.${command.id}.phase must be provisioning or execution.`,
      );
      requireNonEmptyString(command.cwd, `${job.id}.${command.id}.cwd`);
      const commandDirectory = resolve(repositoryRoot, command.cwd);
      requireCondition(
        isInside(repositoryRoot, commandDirectory),
        `${job.id}.${command.id}.cwd must stay inside the repository.`,
      );
      requireNonEmptyString(command.executable, `${job.id}.${command.id}.executable`);
      requireCondition(
        Array.isArray(command.arguments)
          && command.arguments.every((argument) => typeof argument === 'string'),
        `${job.id}.${command.id}.arguments must be an array of strings.`,
      );
    }
  }
}

export function materializeCommand(command, repositoryRoot, jobResultsDirectory) {
  return {
    ...command,
    cwd: resolve(repositoryRoot, command.cwd),
    arguments: command.arguments.map((argument) =>
      argument.replaceAll(RESULTS_PLACEHOLDER, jobResultsDirectory)),
  };
}

function isInside(parent, child) {
  const pathFromParent = relative(resolve(parent), resolve(child));
  return pathFromParent === '' || (!pathFromParent.startsWith('..') && !isAbsolute(pathFromParent));
}

function requireCondition(condition, message) {
  if (!condition) {
    throw new Error(`Invalid PR observation matrix: ${message}`);
  }
}

function requireNonEmptyString(value, name) {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string.`);
}
