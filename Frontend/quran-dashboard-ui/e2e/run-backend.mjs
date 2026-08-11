import { execFileSync, spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdtempSync, rmSync } from 'node:fs';
import { createServer } from 'node:http';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const API_PROJECT = resolve(
  process.cwd(),
  '../../Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj',
);
const tempDirectory = mkdtempSync(join(tmpdir(), 'qdb-e2e-'));
const dumpPath = join(tempDirectory, 'source.dump');
const cloneDatabase = `qdb_e2e_${process.pid}_${randomBytes(4).toString('hex')}`;
const managementClientId = 'e2e-management-client';
const managementClientSecret = randomBytes(24).toString('base64url');
const managementAccessToken = randomBytes(24).toString('base64url');

let backendProcess;
let managementServer;
let databaseCreated = false;
let stopping = false;

process.once('SIGINT', () => stop('SIGINT'));
process.once('SIGTERM', () => stop('SIGTERM'));

try {
  const sourceConnectionString = loadBackendConnectionString();
  const connection = readPostgresConnection(sourceConnectionString);
  const managementApi = await startManagementApiStub();
  managementServer = managementApi.server;
  const postgresEnvironment = {
    ...process.env,
    PGPASSWORD: connection.password,
    PGCONNECT_TIMEOUT: '10',
  };
  const connectionArguments = [
    '--host',
    connection.host,
    '--port',
    connection.port,
    '--username',
    connection.username,
  ];

  const clonedFromTemplate = tryPostgresCommand(
    'createdb',
    [
      ...connectionArguments,
      '--maintenance-db',
      'postgres',
      '--template',
      connection.database,
      cloneDatabase,
    ],
    postgresEnvironment,
  );
  if (clonedFromTemplate) {
    databaseCreated = true;
  } else {
    runPostgresCommand(
      'createdb',
      [
        ...connectionArguments,
        '--maintenance-db',
        'postgres',
        '--template',
        'template0',
        cloneDatabase,
      ],
      postgresEnvironment,
      'create the disposable E2E database',
    );
    databaseCreated = true;
    runPostgresCommand(
      'pg_dump',
      [
        ...connectionArguments,
        '--dbname',
        connection.database,
        '--format',
        'custom',
        '--file',
        dumpPath,
      ],
      postgresEnvironment,
      'snapshot the local source database',
    );
    runPostgresCommand(
      'pg_restore',
      [
        ...connectionArguments,
        '--dbname',
        cloneDatabase,
        '--exit-on-error',
        '--no-owner',
        dumpPath,
      ],
      postgresEnvironment,
      'restore the disposable E2E database',
    );
  }
  rmSync(tempDirectory, { recursive: true, force: true });

  backendProcess = spawn(
    'dotnet',
    ['run', '--project', API_PROJECT, '--no-build', '--no-launch-profile'],
    {
      cwd: process.cwd(),
      env: {
        ...process.env,
        ConnectionStrings__QuranDashboardDb: withDatabase(
          sourceConnectionString,
          cloneDatabase,
        ),
        Auth__ManagementApi__Endpoint: managementApi.endpoint,
        Auth__ManagementApi__Resource: 'e2e-management-api',
        Auth__ManagementApi__AppId: managementClientId,
        Auth__ManagementApi__AppSecret: managementClientSecret,
      },
      stdio: 'inherit',
    },
  );
  backendProcess.once('error', () => finish(1, 'The E2E backend process could not be started.'));
  backendProcess.once('exit', (code) => {
    if (!stopping) {
      finish(code ?? 1);
    }
  });
} catch (error) {
  finish(1, error instanceof Error ? error.message : 'The E2E backend setup failed.');
}

function stop(signal) {
  if (stopping) {
    return;
  }

  stopping = true;
  if (!backendProcess || backendProcess.exitCode !== null || backendProcess.signalCode !== null) {
    finish(0);
    return;
  }

  const forcedShutdown = setTimeout(() => {
    backendProcess.kill('SIGKILL');
  }, 15_000);
  backendProcess.once('exit', () => {
    clearTimeout(forcedShutdown);
    finish(0);
  });
  backendProcess.kill(signal);
  cleanupDatabase();
}

function finish(exitCode, message) {
  if (!stopping) {
    stopping = true;
  }

  cleanupDatabase();
  managementServer?.closeAllConnections();
  managementServer?.close();
  rmSync(tempDirectory, { recursive: true, force: true });
  if (message) {
    console.error(message);
  }
  process.exit(exitCode);
}

function cleanupDatabase() {
  if (!databaseCreated) {
    return;
  }

  try {
    const sourceConnectionString = loadBackendConnectionString();
    const connection = readPostgresConnection(sourceConnectionString);
    runPostgresCommand(
      'dropdb',
      [
        '--host',
        connection.host,
        '--port',
        connection.port,
        '--username',
        connection.username,
        '--force',
        cloneDatabase,
      ],
      {
        ...process.env,
        PGPASSWORD: connection.password,
        PGCONNECT_TIMEOUT: '10',
      },
      'drop the disposable E2E database',
    );
    databaseCreated = false;
  } catch {
    console.error(`Failed to drop disposable E2E database ${cloneDatabase}.`);
  }
}

function loadBackendConnectionString() {
  const configured = process.env.ConnectionStrings__QuranDashboardDb?.trim();
  if (configured) {
    return configured;
  }

  let output;
  try {
    output = execFileSync(
      'dotnet',
      ['user-secrets', 'list', '--json', '--project', API_PROJECT],
      { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] },
    );
  } catch {
    throw new Error(
      'Unable to read the backend user secrets. Set ConnectionStrings__QuranDashboardDb or configure the API user secret.',
    );
  }

  const begin = output.indexOf('//BEGIN');
  const end = output.indexOf('//END');
  if (begin < 0 || end < 0 || end <= begin) {
    throw new Error('The backend user-secrets command returned an unsupported JSON envelope.');
  }

  let secrets;
  try {
    secrets = JSON.parse(output.slice(begin + '//BEGIN'.length, end).trim());
  } catch {
    throw new Error('The backend user-secrets command returned invalid JSON.');
  }

  const connectionString = secrets['ConnectionStrings:QuranDashboardDb'];
  if (typeof connectionString !== 'string' || !connectionString.trim()) {
    throw new Error(
      'ConnectionStrings:QuranDashboardDb is missing from the backend user secrets.',
    );
  }
  return connectionString;
}

function readPostgresConnection(connectionString) {
  const entries = parseConnectionString(connectionString);
  const host = setting(entries, ['host', 'server', 'data source'], 'Host');
  const database = setting(entries, ['database', 'initial catalog'], 'Database');
  const username = setting(entries, ['username', 'user id', 'userid', 'user'], 'Username');
  const password = setting(entries, ['password', 'pwd'], 'Password') ?? '';
  const port = setting(entries, ['port'], 'Port') ?? '5432';

  if (!host || !['localhost', '127.0.0.1', '::1'].includes(host.toLowerCase())) {
    throw new Error('E2E database cloning is restricted to a local PostgreSQL host.');
  }
  if (!database || !username) {
    throw new Error('The backend connection string must include Database and Username.');
  }
  if (!/^\d+$/.test(port)) {
    throw new Error('The backend connection string contains an invalid PostgreSQL port.');
  }

  return { host, port, database, username, password };
}

function withDatabase(connectionString, database) {
  const entries = parseConnectionString(connectionString);
  const matches = entries.filter(({ key }) =>
    ['database', 'initial catalog'].includes(key.toLowerCase()),
  );
  if (matches.length === 0) {
    throw new Error('The backend connection string must include Database.');
  }
  if (matches.length > 1) {
    throw new Error('The backend connection string contains duplicate Database settings.');
  }
  matches[0].value = database;
  return entries.map(({ key, value }) => `${key}=${encodeConnectionValue(value)}`).join(';');
}

function parseConnectionString(connectionString) {
  const entries = [];
  let offset = 0;

  while (offset < connectionString.length) {
    while (connectionString[offset] === ';' || /\s/.test(connectionString[offset] ?? '')) {
      offset += 1;
    }
    if (offset >= connectionString.length) {
      break;
    }

    const equals = connectionString.indexOf('=', offset);
    if (equals < 0) {
      throw new Error('The backend connection string contains an entry without a value.');
    }
    const key = connectionString.slice(offset, equals).trim();
    if (!key) {
      throw new Error('The backend connection string contains a blank key.');
    }
    offset = equals + 1;

    while (/\s/.test(connectionString[offset] ?? '')) {
      offset += 1;
    }

    let value = '';
    const quote = connectionString[offset];
    if (quote === '"' || quote === "'") {
      offset += 1;
      let closed = false;
      while (offset < connectionString.length) {
        const character = connectionString[offset];
        if (character === quote && connectionString[offset + 1] === quote) {
          value += quote;
          offset += 2;
          continue;
        }
        if (character === quote) {
          offset += 1;
          closed = true;
          break;
        }
        value += character;
        offset += 1;
      }
      if (!closed) {
        throw new Error('The backend connection string contains an unterminated quoted value.');
      }
      while (/\s/.test(connectionString[offset] ?? '')) {
        offset += 1;
      }
      if (offset < connectionString.length && connectionString[offset] !== ';') {
        throw new Error('The backend connection string contains trailing text after a quoted value.');
      }
    } else {
      const semicolon = connectionString.indexOf(';', offset);
      const end = semicolon < 0 ? connectionString.length : semicolon;
      value = connectionString.slice(offset, end).trim();
      offset = end;
    }

    entries.push({ key, value });
    if (connectionString[offset] === ';') {
      offset += 1;
    }
  }

  return entries;
}

function setting(entries, aliases, semanticName) {
  const matches = entries.filter(({ key }) => aliases.includes(key.toLowerCase()));
  if (matches.length > 1) {
    throw new Error(
      `The backend connection string contains duplicate ${semanticName} settings.`,
    );
  }
  return matches[0]?.value;
}

function encodeConnectionValue(value) {
  if (!value || /[;"']|^\s|\s$/.test(value)) {
    return `"${value.replaceAll('"', '""')}"`;
  }
  return value;
}

function runPostgresCommand(command, arguments_, environment, operation) {
  try {
    execFileSync(command, arguments_, {
      env: environment,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
  } catch {
    throw new Error(`Failed to ${operation}.`);
  }
}

function tryPostgresCommand(command, arguments_, environment) {
  try {
    execFileSync(command, arguments_, {
      env: environment,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    return true;
  } catch {
    return false;
  }
}

function startManagementApiStub() {
  return new Promise((resolvePromise, rejectPromise) => {
    const server = createServer((request, response) => {
      const url = new URL(request.url ?? '/', 'http://127.0.0.1');
      if (request.method === 'POST' && url.pathname === '/oidc/token') {
        request.resume();
        const expectedAuthorization = `Basic ${Buffer.from(
          `${managementClientId}:${managementClientSecret}`,
        ).toString('base64')}`;
        if (request.headers.authorization !== expectedAuthorization) {
          sendJson(response, 401, { error: 'invalid_client' });
          return;
        }
        sendJson(response, 200, {
          access_token: managementAccessToken,
          expires_in: 3600,
          token_type: 'Bearer',
        });
        return;
      }

      if (request.method === 'GET' && url.pathname.startsWith('/api/users/')) {
        if (request.headers.authorization !== `Bearer ${managementAccessToken}`) {
          sendJson(response, 401, { error: 'invalid_token' });
          return;
        }
        const subject = decodeURIComponent(url.pathname.slice('/api/users/'.length));
        if (!/^e2e-[a-z0-9-]+$/.test(subject) || subject.length > 64) {
          sendJson(response, 404, { error: 'user_not_found' });
          return;
        }
        sendJson(response, 200, {
          primaryEmail: `${subject}@example.test`,
          username: subject,
          name: 'E2E test persona',
        });
        return;
      }

      sendJson(response, 404, { error: 'not_found' });
    });

    server.once('error', rejectPromise);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      if (!address || typeof address === 'string') {
        server.close();
        rejectPromise(new Error('The local E2E Management API stub did not expose a TCP port.'));
        return;
      }
      server.removeListener('error', rejectPromise);
      resolvePromise({ server, endpoint: `http://127.0.0.1:${address.port}` });
    });
  });
}

function sendJson(response, statusCode, body) {
  response.writeHead(statusCode, { 'content-type': 'application/json' });
  response.end(JSON.stringify(body));
}
