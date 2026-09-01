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
       IF EXISTS (SELECT 1 FROM roles) THEN
         RAISE EXCEPTION 'Access administration precondition requires an empty roles table.';
       END IF;
     END
     $$;
     INSERT INTO roles (id, name, display_name) VALUES (1, 'Owner', 'المالك');
     SELECT setval(pg_get_serial_sequence('roles', 'id'), 1, true);
     SELECT concat_ws(',', id, name, display_name) FROM roles;`,
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

if (!result.endsWith('1,Owner,المالك')) {
  throw new Error(`Access administration precondition produced an unexpected Owner role: ${result}`);
}

console.log('[e2e] access administration precondition seeded canonical Owner role');
