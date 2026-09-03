import assert from 'node:assert/strict';

import {
  classifyPlaywrightPolicy,
  validatePlaywrightPolicyContract,
} from './playwright-policy-contract.mjs';

const contract = {
  schemaVersion: 1,
  fixtureProfiles: {
    canonical: {
      setupWrites: [],
      resetBehavior: 'none',
      databaseTarget: 'test-database',
      startupEffects: ['read-only-api'],
    },
    guarded: {
      setupWrites: [],
      resetBehavior: 'none',
      databaseTarget: 'test-database',
      startupEffects: ['read-only-api'],
    },
    authenticated: {
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

console.log('Playwright policy contract passed.');
