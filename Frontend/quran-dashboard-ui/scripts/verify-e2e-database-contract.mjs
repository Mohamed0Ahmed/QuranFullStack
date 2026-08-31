import assert from 'node:assert/strict';

import {
  MUTABLE_TABLES,
  assertLoopbackConnection,
  assertRuntimeConnection,
  parsePostgresConnectionString,
  resolveDatabaseMode,
} from '../e2e/harness/database-contract.mjs';

assert.equal(
  resolveDatabaseMode({ ConnectionStrings__QuranDashboardDb: 'Host=remote.example.test' }),
  'artifact',
  'artifact mode must remain the default even when an ambient connection string exists',
);

assert.equal(
  resolveDatabaseMode({ E2E_DATABASE_MODE: 'clone-local' }),
  'clone-local',
  'clone-local must remain an explicit local opt-in',
);

assert.throws(
  () => resolveDatabaseMode({ CI: 'true', E2E_DATABASE_MODE: 'clone-local' }),
  /CI.*artifact/i,
  'CI must reject clone-local mode',
);
assert.throws(
  () => resolveDatabaseMode({ E2E_DATABASE_MODE: 'ambient-secret' }),
  /unsupported.*database mode/i,
  'unknown modes must fail closed',
);

const localConnection = parsePostgresConnectionString(
  'Host=127.0.0.1;Port=5432;Database=quran_dashboard;Username=postgres;Password="semi;colon"',
);
assert.equal(localConnection.password, 'semi;colon');
assert.doesNotThrow(() => assertLoopbackConnection(localConnection));

const remoteConnection = parsePostgresConnectionString(
  'Host=db.example.test;Database=quran_dashboard;Username=postgres;Password=test',
);
assert.doesNotThrow(() =>
  assertRuntimeConnection(
    { host: '172.20.0.2', port: '5432', database: 'qdb_e2e', username: 'postgres' },
    'artifact',
  ),
);
assert.throws(
  () => assertRuntimeConnection(remoteConnection, 'artifact'),
  /internal Docker network/i,
  'artifact mode must reject public or shared database addresses',
);
assert.throws(
  () => assertLoopbackConnection(remoteConnection),
  /loopback/i,
  'clone-local must reject remote and shared databases',
);

assert.ok(MUTABLE_TABLES.length > 0, 'the reset contract must declare an explicit allowlist');
assert.equal(
  new Set(MUTABLE_TABLES).size,
  MUTABLE_TABLES.length,
  'the mutable allowlist must not contain duplicates',
);
assert.equal(
  MUTABLE_TABLES.some((table) => table.startsWith('quran_')),
  false,
  'Quran and PhraseSearch tables must never enter the mutable reset allowlist',
);
assert.equal(
  MUTABLE_TABLES.includes('permissions'),
  false,
  'the synchronized Permission catalogue is fixture infrastructure, not scenario state',
);

console.log('E2E database mode and reset contract passed.');
