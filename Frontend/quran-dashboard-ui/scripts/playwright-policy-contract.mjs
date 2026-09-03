import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { isAbsolute, relative, resolve, sep } from 'node:path';

const STATE_POLICIES = new Set(['canonical-read', 'guarded-read', 'mutating']);
const DATABASE_TARGETS = new Set(['test-database']);
const RESET_BEHAVIORS = new Set(['none', 'mutable-application-state']);
const SETUP_WRITES = new Set(['mutable-application-state']);
const STARTUP_EFFECTS = new Set(['read-only-api', 'mutable-api']);
const POLICY_SEVERITY = new Map([
  ['canonical-read', 0],
  ['guarded-read', 1],
  ['mutating', 2],
]);
const EXECUTION_GROUP = new Map([
  ['canonical-read', 'CanonicalReader'],
  ['guarded-read', 'GuardedReader'],
  ['mutating', 'MutableWriter'],
]);
const SHA256_PATTERN = /^[0-9a-f]{64}$/;

export function loadPlaywrightPolicyContract(path, e2eRoot) {
  let contract;
  try {
    contract = JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read Playwright policy contract ${path}: ${error.message}`);
  }

  validatePlaywrightPolicyContract(contract, e2eRoot);
  return contract;
}

export function validatePlaywrightPolicyContract(contract, e2eRoot) {
  requireCondition(contract?.schemaVersion === 1, 'schemaVersion must be 1');
  requireCondition(
    isObject(contract.fixtureProfiles) && Object.keys(contract.fixtureProfiles).length > 0,
    'fixtureProfiles must be a non-empty object',
  );

  for (const [name, profile] of Object.entries(contract.fixtureProfiles)) {
    requireCondition(/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(name), `invalid fixture profile ${name}`);
    requireStringArray(profile.setupWrites, `${name}.setupWrites`, SETUP_WRITES);
    requireCondition(
      RESET_BEHAVIORS.has(profile.resetBehavior),
      `${name}.resetBehavior is unsupported`,
    );
    requireCondition(
      DATABASE_TARGETS.has(profile.databaseTarget),
      `${name}.databaseTarget is unsupported`,
    );
    requireStringArray(profile.startupEffects, `${name}.startupEffects`, STARTUP_EFFECTS);

    if (profile.startupEffects.includes('read-only-api')) {
      requireCondition(
        !profile.startupEffects.includes('mutable-api'),
        `${name} has contradictory API startup effects`,
      );
    }
  }

  requireCondition(
    Array.isArray(contract.migrationInventory),
    'migrationInventory must be an array',
  );
  const inventoryFiles = new Set();
  for (const entry of contract.migrationInventory) {
    requireCondition(isObject(entry), 'migration inventory entries must be objects');
    requireCondition(
      typeof entry.file === 'string' && entry.file.endsWith('.e2e.ts'),
      'migration inventory file must identify an .e2e.ts source',
    );
    requireCondition(!isAbsolute(entry.file), `migration inventory path must be relative: ${entry.file}`);
    requireCondition(
      !entry.file.split(/[\\/]/).includes('..'),
      `migration inventory path cannot escape the E2E root: ${entry.file}`,
    );
    requireCondition(!inventoryFiles.has(entry.file), `duplicate migration inventory file: ${entry.file}`);
    requireCondition(SHA256_PATTERN.test(entry.sha256), `invalid SHA-256 for ${entry.file}`);
    inventoryFiles.add(entry.file);

    const absolutePath = resolve(e2eRoot, entry.file);
    requireCondition(isInside(e2eRoot, absolutePath), `migration inventory path escapes E2E root: ${entry.file}`);
    const actualHash = sha256(readFileSync(absolutePath));
    requireCondition(
      actualHash === entry.sha256,
      `unmigrated Playwright source changed without policy migration: ${entry.file}`,
    );
  }
}

export function classifyPlaywrightPolicy(annotations, contract, location) {
  const statePolicies = annotations.filter(({ type }) => STATE_POLICIES.has(type));
  if (statePolicies.length !== 1) {
    throw policyError(location, 'must declare exactly one state policy: canonical-read, guarded-read, or mutating');
  }

  const fixtureAnnotations = annotations.filter(({ type }) => type === 'fixture-policy');
  if (fixtureAnnotations.length !== 1 || !fixtureAnnotations[0].description) {
    throw policyError(location, 'must declare exactly one fixture-policy annotation');
  }

  const declaredPolicy = statePolicies[0].type;
  const fixtureProfile = fixtureAnnotations[0].description;
  const fixture = contract.fixtureProfiles[fixtureProfile];
  if (!fixture) {
    throw policyError(location, `references unknown fixture policy ${fixtureProfile}`);
  }

  const fixturePolicy = minimumFixturePolicy(fixture);
  if (POLICY_SEVERITY.get(fixturePolicy) > POLICY_SEVERITY.get(declaredPolicy)) {
    throw policyError(
      location,
      `is under-classified as ${declaredPolicy}; fixture ${fixtureProfile} requires ${fixturePolicy}`,
    );
  }

  return {
    declaredPolicy,
    effectiveGroup: EXECUTION_GROUP.get(declaredPolicy),
    fixtureProfile,
  };
}

export function requireLegacyMigrationEntry(file, e2eRoot, contract, location) {
  const relativeFile = normalize(relative(e2eRoot, file));
  const entry = contract.migrationInventory.find((candidate) => candidate.file === relativeFile);
  if (!entry) {
    throw policyError(location, 'is missing required state policy and is not in the migration inventory');
  }

  const actualHash = sha256(readFileSync(file));
  if (actualHash !== entry.sha256) {
    throw policyError(
      location,
      `belongs to changed unmigrated source ${relativeFile}; classify it or update the explicit migration inventory`,
    );
  }

  return {
    declaredPolicy: 'legacy-unmigrated',
    effectiveGroup: 'LegacyUnmigrated',
    fixtureProfile: null,
  };
}

export function isNewPolicyAnnotation(annotation) {
  return annotation.type === 'canonical-read'
    || annotation.type === 'guarded-read'
    || annotation.type === 'fixture-policy';
}

function minimumFixturePolicy(fixture) {
  if (
    fixture.setupWrites.includes('mutable-application-state')
    || fixture.resetBehavior === 'mutable-application-state'
    || fixture.startupEffects.includes('mutable-api')
  ) {
    return 'mutating';
  }
  return 'canonical-read';
}

function requireStringArray(value, name, allowed) {
  requireCondition(Array.isArray(value), `${name} must be an array`);
  requireCondition(value.every((item) => allowed.has(item)), `${name} contains an unsupported value`);
  requireCondition(new Set(value).size === value.length, `${name} contains duplicate values`);
}

function sha256(value) {
  return createHash('sha256').update(value).digest('hex');
}

function normalize(path) {
  return path.split(sep).join('/');
}

function isInside(parent, child) {
  const pathFromParent = relative(resolve(parent), resolve(child));
  return pathFromParent === '' || (!pathFromParent.startsWith('..') && !isAbsolute(pathFromParent));
}

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function requireCondition(condition, message) {
  if (!condition) {
    throw new Error(`Invalid Playwright policy contract: ${message}.`);
  }
}

function policyError(location, message) {
  return new Error(`Playwright policy validation failed: ${location} ${message}`);
}
