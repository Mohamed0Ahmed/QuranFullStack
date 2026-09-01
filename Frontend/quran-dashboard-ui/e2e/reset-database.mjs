import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
  MUTABLE_TABLES,
  assertRuntimeConnection,
  parsePostgresConnectionString,
} from './harness/database-contract.mjs';
import { immutableDatabaseFingerprint } from './harness/database-runtime.mjs';

const runtimeStatePath = resolve(process.cwd(), '.playwright/e2e-database.json');
const runtime = JSON.parse(readFileSync(runtimeStatePath, 'utf8'));
if (!['artifact', 'clone-local'].includes(runtime.mode)) {
  throw new Error('The E2E database runtime state contains an unsupported mode.');
}
if (typeof runtime.connectionString !== 'string' || !runtime.connectionString.trim()) {
  throw new Error('The E2E database runtime state is missing its connection string.');
}
if (!/^[0-9a-f]{64}$/.test(runtime.immutableSha256)) {
  throw new Error('The E2E database runtime state is missing its immutable baseline fingerprint.');
}

const connection = parsePostgresConnectionString(runtime.connectionString);
assertRuntimeConnection(connection, runtime.mode);
assertBackgroundWorkersIdle('before reset');
const before = await immutableDatabaseFingerprint(connection);
if (before !== runtime.immutableSha256) {
  throw new Error(
    'Scenario execution changed immutable Quran or PhraseSearch state; refusing to reset over the corruption.',
  );
}

runPsql([
  '--set',
  'ON_ERROR_STOP=1',
  '--command',
  `BEGIN; TRUNCATE TABLE ${MUTABLE_TABLES.map(quoteIdentifier).join(', ')} RESTART IDENTITY; COMMIT;`,
]);

const counts = runPsql([
  '--tuples-only',
  '--no-align',
  '--command',
  `SELECT concat_ws(',', ${MUTABLE_TABLES.map(
    (table) => `(SELECT count(*) FROM ${quoteIdentifier(table)})`,
  ).join(', ')});`,
]).trim();
if (counts.split(',').some((count) => count !== '0')) {
  throw new Error(`Mutable E2E reset left non-empty allowlisted tables: ${counts}.`);
}
assertBackgroundWorkersIdle('after reset');

const after = await immutableDatabaseFingerprint(connection);
if (after !== runtime.immutableSha256) {
  throw new Error(
    'Mutable E2E reset changed immutable Quran or PhraseSearch state; refusing to continue.',
  );
}

console.log(
  `[e2e] mutable reset verified tables=${MUTABLE_TABLES.length} background=idle immutableSha256=${after}`,
);

function assertBackgroundWorkersIdle(stage) {
  const active = runPsql([
    '--tuples-only',
    '--no-align',
    '--command',
    `SELECT
       (SELECT count(*) FROM linking_confirmation_jobs
        WHERE status IN ('queued', 'running', 'finalizing'))
       +
       (SELECT count(*) FROM linking_prepared_preflights
        WHERE status IN ('queued', 'preparing'));`,
  ]).trim();
  if (active !== '0') {
    throw new Error(`E2E background work is not drained ${stage}: active=${active}.`);
  }
}

function runPsql(arguments_) {
  return execFileSync(
    'psql',
    [...connectionArguments(), '--dbname', connection.database, ...arguments_],
    {
      env: postgresEnvironment(),
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    },
  );
}

function connectionArguments() {
  return [
    '--host',
    connection.host,
    '--port',
    connection.port,
    '--username',
    connection.username,
  ];
}

function postgresEnvironment() {
  return {
    ...process.env,
    PGPASSWORD: connection.password,
    PGCONNECT_TIMEOUT: '10',
    PGOPTIONS: '-c statement_timeout=10000',
  };
}

function quoteIdentifier(identifier) {
  if (!/^[a-z][a-z0-9_]{0,62}$/.test(identifier)) {
    throw new Error(`Unsafe mutable table identifier: ${identifier}.`);
  }
  return `"${identifier}"`;
}
