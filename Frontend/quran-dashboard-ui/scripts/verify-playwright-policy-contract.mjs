import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
  classifyPlaywrightPolicy,
  validatePlaywrightPolicyContract,
} from './playwright-policy-contract.mjs';

const contract = {
  schemaVersion: 1,
  fixtureProfiles: {
    canonical: {
      backgroundActivities: [],
      setupWrites: [],
      resetBehavior: 'none',
      databaseTarget: 'test-database',
      startupEffects: ['read-only-api'],
    },
    guarded: {
      backgroundActivities: [],
      setupWrites: [],
      resetBehavior: 'none',
      databaseTarget: 'test-database',
      startupEffects: ['read-only-api'],
    },
    authenticated: {
      backgroundActivities: ['LinkingConfirmationJobProcessor'],
      setupWrites: ['mutable-application-state'],
      resetBehavior: 'mutable-application-state',
      databaseTarget: 'test-database',
      startupEffects: ['mutable-api'],
    },
  },
  migrationInventory: [],
};

validatePlaywrightPolicyContract(contract, '/repository/e2e');

assert.deepEqual(
  classifyPlaywrightPolicy(
    [{ type: 'canonical-read' }, { type: 'fixture-policy', description: 'canonical' }],
    contract,
    'reader.e2e.ts:12',
  ),
  {
    backgroundActivities: [],
    declaredPolicy: 'canonical-read',
    effectiveGroup: 'CanonicalReader',
    fixtureProfile: 'canonical',
  },
);

assert.deepEqual(
  classifyPlaywrightPolicy(
    [{ type: 'guarded-read' }, { type: 'fixture-policy', description: 'guarded' }],
    contract,
    'guarded.e2e.ts:20',
  ),
  {
    backgroundActivities: [],
    declaredPolicy: 'guarded-read',
    effectiveGroup: 'GuardedReader',
    fixtureProfile: 'guarded',
  },
);

assert.deepEqual(
  classifyPlaywrightPolicy(
    [{ type: 'mutating' }, { type: 'fixture-policy', description: 'authenticated' }],
    contract,
    'writer.e2e.ts:30',
  ),
  {
    backgroundActivities: ['LinkingConfirmationJobProcessor'],
    declaredPolicy: 'mutating',
    effectiveGroup: 'MutableWriter',
    fixtureProfile: 'authenticated',
  },
);

assert.throws(
  () =>
    classifyPlaywrightPolicy(
      [{ type: 'canonical-read' }, { type: 'fixture-policy', description: 'authenticated' }],
      contract,
      'reader.e2e.ts:40',
    ),
  /under-classified.*canonical-read.*authenticated.*mutating/i,
);

assert.throws(
  () =>
    classifyPlaywrightPolicy(
      [
        { type: 'canonical-read' },
        { type: 'guarded-read' },
        { type: 'fixture-policy', description: 'canonical' },
      ],
      contract,
      'contradictory.e2e.ts:50',
    ),
  /exactly one.*state policy/i,
);

assert.throws(
  () => classifyPlaywrightPolicy([], contract, 'missing.e2e.ts:60'),
  /missing.*state policy/i,
);

assert.throws(
  () => validatePlaywrightPolicyContract({
    ...contract,
    fixtureProfiles: {
      ...contract.fixtureProfiles,
      guarded: {
        ...contract.fixtureProfiles.guarded,
        backgroundActivities: ['LinkingPreparedPreflightProcessor'],
      },
    },
  }, '/repository/e2e'),
  /read-only.*background activity/i,
);

const repositoryContract = JSON.parse(
  readFileSync(resolve(process.cwd(), 'e2e/playwright-policy.json'), 'utf8'),
);
validatePlaywrightPolicyContract(repositoryContract, resolve(process.cwd(), 'e2e'));
assert.deepEqual(
  repositoryContract.migrationInventory,
  [],
  'every Playwright source must use the executable state policy contract',
);

console.log('Playwright policy contract passed.');
