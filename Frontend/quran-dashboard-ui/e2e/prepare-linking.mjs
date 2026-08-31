import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
  assertRuntimeConnection,
  parsePostgresConnectionString,
} from './harness/database-contract.mjs';

const runtimeStatePath = resolve(process.cwd(), '.playwright/e2e-database.json');
const runtime = JSON.parse(readFileSync(runtimeStatePath, 'utf8'));
if (!['artifact', 'clone-local'].includes(runtime.mode)) {
  throw new Error('The E2E database runtime state contains an unsupported mode.');
}
if (typeof runtime.connectionString !== 'string' || !runtime.connectionString.trim()) {
  throw new Error('The E2E database runtime state is missing its connection string.');
}

const connection = parsePostgresConnectionString(runtime.connectionString);
assertRuntimeConnection(connection, runtime.mode);
const result = execFileSync(
  'psql',
  [
    '--host',
    connection.host,
    '--port',
    connection.port,
    '--username',
    connection.username,
    '--dbname',
    connection.database,
    '--set',
    'ON_ERROR_STOP=1',
    '--tuples-only',
    '--no-align',
    '--command',
    `DO $$
     BEGIN
       IF EXISTS (SELECT 1 FROM linking_data_state) THEN
         RAISE EXCEPTION 'Linking precondition requires an empty linking_data_state table.';
       END IF;
     END
     $$;
     INSERT INTO linking_data_state (id, generation, updated_at_utc)
     VALUES (1, 1, to_timestamp(0));
     SELECT concat_ws(',', id, generation, extract(epoch FROM updated_at_utc)::bigint)
     FROM linking_data_state;`,
  ],
  {
    env: {
      ...process.env,
      PGPASSWORD: connection.password,
      PGCONNECT_TIMEOUT: '10',
      PGOPTIONS: '-c statement_timeout=10000',
    },
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  },
).trim();

if (!result.endsWith('1,1,0')) {
  throw new Error(`Linking precondition produced an unexpected revision state: ${result}`);
}

console.log('[e2e] Linking precondition seeded canonical revision generation=1');
