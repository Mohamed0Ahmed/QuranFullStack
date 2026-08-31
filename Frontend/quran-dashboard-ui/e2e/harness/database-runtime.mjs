import { execFileSync, spawn } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import {
  mkdirSync,
  mkdtempSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';

import {
  COMPACT_ARTIFACT_IDS,
  COMPACT_BASE_ARTIFACT_ID,
  COMPACT_PHRASE_READY_ARTIFACT_ID,
} from './artifact-contract.mjs';
import {
  assertLoopbackConnection,
  assertRuntimeConnection,
  parsePostgresConnectionString,
  resolveDatabaseMode,
  withDatabase,
} from './database-contract.mjs';

const FRONTEND_ROOT = process.cwd();
const REPOSITORY_ROOT = resolve(FRONTEND_ROOT, '../..');
const ARTIFACT_TOOL = resolve(REPOSITORY_ROOT, 'Backend/scripts/test-artifacts');
const ARTIFACT_LOCK = resolve(REPOSITORY_ROOT, 'test-artifacts.lock.json');
const RUNTIME_STATE_PATH = resolve(FRONTEND_ROOT, '.playwright/e2e-database.json');

export async function provisionDatabaseRuntime(apiProject) {
  const mode = resolveDatabaseMode();
  const runtime = mode === 'artifact'
    ? await provisionArtifactDatabase()
    : provisionCloneLocalDatabase(apiProject);
  try {
    runtime.immutableSha256 = await immutableDatabaseFingerprint(runtime.connection);
    return runtime;
  } catch (error) {
    runtime.cleanup();
    throw error;
  }
}

export function writeDatabaseRuntimeState(runtime) {
  mkdirSync(dirname(RUNTIME_STATE_PATH), { recursive: true });
  rmSync(RUNTIME_STATE_PATH, { force: true });
  writeFileSync(
    RUNTIME_STATE_PATH,
    `${JSON.stringify({
      mode: runtime.mode,
      connectionString: runtime.connectionString,
      immutableSha256: runtime.immutableSha256,
    })}\n`,
    { encoding: 'utf8', mode: 0o600 },
  );
}

export function readPreparedDatabaseRuntime() {
  let state;
  try {
    state = JSON.parse(readFileSync(RUNTIME_STATE_PATH, 'utf8'));
  } catch {
    throw new Error('Prepared E2E database state is missing or invalid.');
  }
  if (state.mode !== 'artifact' || typeof state.connectionString !== 'string') {
    throw new Error('Sealed execution accepts only a prepared artifact database.');
  }
  const connection = parsePostgresConnectionString(state.connectionString);
  assertRuntimeConnection(connection, state.mode);
  if (!/^[a-f0-9]{64}$/.test(state.immutableSha256)) {
    throw new Error('Prepared E2E database state has no valid immutable fingerprint.');
  }
  return {
    mode: state.mode,
    connection,
    connectionString: state.connectionString,
    immutableSha256: state.immutableSha256,
    cleanup() {},
  };
}

export function removeDatabaseRuntimeState() {
  rmSync(RUNTIME_STATE_PATH, { force: true });
}

export function immutableDatabaseFingerprint(connection) {
  return new Promise((resolvePromise, rejectPromise) => {
    const hash = createHash('sha256');
    let standardError = '';
    const dump = spawn(
      'pg_dump',
      [
        ...connectionArguments(connection),
        '--dbname',
        connection.database,
        '--data-only',
        '--no-owner',
        '--no-privileges',
        '--restrict-key=qdbE2EImmutableFingerprintV1',
        '--table=quran_*',
      ],
      {
        env: postgresEnvironment(connection),
        stdio: ['ignore', 'pipe', 'pipe'],
      },
    );
    dump.stdout.on('data', (chunk) => hash.update(chunk));
    dump.stderr.setEncoding('utf8');
    dump.stderr.on('data', (chunk) => {
      standardError += chunk;
    });
    dump.once('error', rejectPromise);
    dump.once('close', (code) => {
      if (code !== 0) {
        rejectPromise(
          new Error(`Failed to fingerprint immutable E2E data: ${standardError.trim()}`),
        );
        return;
      }
      resolvePromise(hash.digest('hex'));
    });
  });
}

async function provisionArtifactDatabase() {
  verifyArtifact();

  const artifacts = COMPACT_ARTIFACT_IDS.map(readLockedArtifact);
  const baseArtifact = readLockedArtifact(COMPACT_BASE_ARTIFACT_ID);
  const phraseReadyArtifact = readLockedArtifact(COMPACT_PHRASE_READY_ARTIFACT_ID);
  const digests = new Set(artifacts.map((artifact) => artifact.postgresql.containerDigest));
  if (digests.size !== 1) {
    throw new Error('Composable compact E2E artifacts must lock the same PostgreSQL image digest.');
  }
  const image = `postgres@${baseArtifact.postgresql.containerDigest}`;
  try {
    execFileSync('docker', ['image', 'inspect', image], { stdio: 'ignore' });
  } catch {
    throw new Error(
      `The pinned PostgreSQL image is not preloaded: ${image}. Artifact execution never pulls implicitly.`,
    );
  }

  const suffix = `${process.pid}-${randomBytes(4).toString('hex')}`;
  const containerName = `qdb-e2e-artifact-${suffix}`;
  const networkName = `qdb-e2e-internal-${suffix}`;
  const password = randomBytes(24).toString('base64url');
  const database = 'qdb_e2e';
  let containerStarted = false;
  let networkCreated = false;

  try {
    execFileSync('docker', ['network', 'create', '--internal', networkName], {
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    networkCreated = true;
    execFileSync(
      'docker',
      [
        'run',
        '--detach',
        '--rm',
        '--pull=never',
        '--name',
        containerName,
        '--network',
        networkName,
        '--env',
        `POSTGRES_PASSWORD=${password}`,
        '--env',
        `POSTGRES_DB=${database}`,
        image,
      ],
      { stdio: ['ignore', 'pipe', 'pipe'] },
    );
    containerStarted = true;

    const containerHost = execFileSync(
      'docker',
      [
        'inspect',
        '--format',
        '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}',
        containerName,
      ],
      {
        encoding: 'utf8',
        stdio: ['ignore', 'pipe', 'pipe'],
      },
    ).trim();

    const connection = {
      host: containerHost,
      port: '5432',
      database,
      username: 'postgres',
      password,
    };
    if (process.env.E2E_SEALED_EXECUTION === '1') {
      process.env.QDB_E2E_ALLOWED_IPV4 = containerHost;
    }
    assertRuntimeConnection(connection, 'artifact');
    await waitForPostgres(connection);
    runPostgresCommand(
      'pg_restore',
      [
        ...connectionArguments(connection),
        '--dbname',
        database,
        '--exit-on-error',
        '--no-owner',
        '--no-privileges',
        resolve(REPOSITORY_ROOT, artifactPayloadPath(baseArtifact)),
      ],
      postgresEnvironment(connection),
      'restore the verified compact cross-stack base artifact',
    );
    runPostgresCommand(
      'pg_restore',
      [
        ...connectionArguments(connection),
        '--dbname',
        database,
        '--exit-on-error',
        '--no-owner',
        '--no-privileges',
        resolve(REPOSITORY_ROOT, artifactPayloadPath(phraseReadyArtifact)),
      ],
      postgresEnvironment(connection),
      'restore the verified compact PhraseSearch-ready overlay artifact',
    );
    verifyPhraseSearchRuntime(connection, phraseReadyArtifact);

    return {
      mode: 'artifact',
      connection,
      connectionString: formatConnectionString(connection),
      containerName,
      cleanup: once(() => {
        if (containerStarted) {
          tryCommand('docker', ['rm', '--force', containerName], process.env);
          containerStarted = false;
        }
        if (networkCreated) {
          tryCommand('docker', ['network', 'rm', networkName], process.env);
          networkCreated = false;
        }
      }),
    };
  } catch (error) {
    if (containerStarted) {
      tryCommand('docker', ['rm', '--force', containerName], process.env);
    }
    if (networkCreated) {
      tryCommand('docker', ['network', 'rm', networkName], process.env);
    }
    throw error;
  }
}

function verifyArtifact() {
  if (process.env.E2E_SEALED_EXECUTION === '1') {
    const verifierAssembly = process.env.E2E_ARTIFACT_VERIFIER_ASSEMBLY?.trim();
    if (!verifierAssembly) {
      throw new Error('Sealed execution requires a receipt-bound artifact verifier assembly.');
    }
    for (const artifactId of COMPACT_ARTIFACT_IDS) {
      execFileSync(
        'dotnet',
        [
          verifierAssembly,
          'verify',
          '--artifact',
          artifactId,
          '--root',
          REPOSITORY_ROOT,
        ],
        { cwd: REPOSITORY_ROOT, stdio: 'inherit' },
      );
    }
    return;
  }

  for (const artifactId of COMPACT_ARTIFACT_IDS) {
    execFileSync(ARTIFACT_TOOL, ['verify', '--artifact', artifactId], {
      cwd: REPOSITORY_ROOT,
      stdio: 'inherit',
    });
  }
}

function provisionCloneLocalDatabase(apiProject) {
  const sourceConnectionString = loadBackendConnectionString(apiProject);
  const connection = parsePostgresConnectionString(sourceConnectionString);
  assertLoopbackConnection(connection);

  const cloneDatabase = `qdb_e2e_${process.pid}_${randomBytes(4).toString('hex')}`;
  const tempDirectory = mkdtempSync(join(tmpdir(), 'qdb-e2e-clone-'));
  const dumpPath = join(tempDirectory, 'source.dump');
  let databaseCreated = false;
  const environment = postgresEnvironment(connection);
  const arguments_ = connectionArguments(connection);

  try {
    const clonedFromTemplate = tryCommand(
      'createdb',
      [
        ...arguments_,
        '--maintenance-db',
        'postgres',
        '--template',
        connection.database,
        cloneDatabase,
      ],
      environment,
    );
    if (clonedFromTemplate) {
      databaseCreated = true;
    } else {
      runPostgresCommand(
        'createdb',
        [
          ...arguments_,
          '--maintenance-db',
          'postgres',
          '--template',
          'template0',
          cloneDatabase,
        ],
        environment,
        'create the disposable clone-local E2E database',
      );
      databaseCreated = true;
      runPostgresCommand(
        'pg_dump',
        [
          ...arguments_,
          '--dbname',
          connection.database,
          '--format',
          'custom',
          '--file',
          dumpPath,
        ],
        environment,
        'snapshot the explicit loopback source database',
      );
      runPostgresCommand(
        'pg_restore',
        [
          ...arguments_,
          '--dbname',
          cloneDatabase,
          '--exit-on-error',
          '--no-owner',
          dumpPath,
        ],
        environment,
        'restore the disposable clone-local E2E database',
      );
    }

    const cloneConnectionString = withDatabase(sourceConnectionString, cloneDatabase);
    const cloneConnection = parsePostgresConnectionString(cloneConnectionString);
    return {
      mode: 'clone-local',
      connection: cloneConnection,
      connectionString: cloneConnectionString,
      cleanup: once(() => {
        if (databaseCreated) {
          tryCommand(
            'dropdb',
            [
              ...arguments_,
              '--maintenance-db',
              'postgres',
              '--force',
              cloneDatabase,
            ],
            environment,
          );
          databaseCreated = false;
        }
        rmSync(tempDirectory, { recursive: true, force: true });
      }),
    };
  } catch (error) {
    if (databaseCreated) {
      tryCommand(
        'dropdb',
        [...arguments_, '--maintenance-db', 'postgres', '--force', cloneDatabase],
        environment,
      );
    }
    rmSync(tempDirectory, { recursive: true, force: true });
    throw error;
  }
}

function readLockedArtifact(artifactId) {
  const artifactLock = JSON.parse(readFileSync(ARTIFACT_LOCK, 'utf8'));
  const artifacts = Array.isArray(artifactLock.artifacts) ? artifactLock.artifacts : [];
  const matches = artifacts.filter((artifact) => artifact.id === artifactId);
  if (matches.length !== 1) {
    throw new Error(`Expected one locked artifact named ${artifactId}.`);
  }
  return matches[0];
}

function artifactPayloadPath(artifact) {
  const payloads = artifact.stagedFiles.filter((file) => file.role === 'payload');
  if (payloads.length !== 1) {
    throw new Error(`Artifact ${artifact.id} must expose exactly one database payload.`);
  }
  return payloads[0].path;
}

function verifyPhraseSearchRuntime(connection, artifact) {
  const manifest = JSON.parse(
    readFileSync(resolve(REPOSITORY_ROOT, artifact.manifestPath), 'utf8'),
  );
  const expected = manifest.phraseSearch;
  if (
    typeof expected?.activeBuildId !== 'string'
    || typeof expected.sourceFingerprint !== 'string'
    || expected.readiness !== 'available'
  ) {
    throw new Error('The PhraseSearch-ready manifest has no complete runtime expectation.');
  }

  let actual;
  try {
    actual = execFileSync(
      'psql',
      [
        ...connectionArguments(connection),
        '--dbname',
        connection.database,
        '--set',
        'ON_ERROR_STOP=1',
        '--tuples-only',
        '--no-align',
        '--command',
        `SELECT concat_ws('|', state.active_build_id, state.source_fingerprint,
          state.is_stale, build.source_fingerprint, build.status::int,
          build.exact_ready, build.similarity_ready)
         FROM quran_phrase_index_state AS state
         INNER JOIN quran_phrase_index_builds AS build ON build.id = state.active_build_id
         WHERE state.id = 1;`,
      ],
      {
        encoding: 'utf8',
        env: postgresEnvironment(connection),
        stdio: ['ignore', 'pipe', 'pipe'],
      },
    ).trim();
  } catch {
    throw new Error('Failed to verify the composed PhraseSearch runtime identity.');
  }

  const expectedRuntime = [
    expected.activeBuildId,
    expected.sourceFingerprint,
    'f',
    expected.sourceFingerprint,
    '3',
    't',
    't',
  ].join('|');
  if (actual !== expectedRuntime) {
    throw new Error('The composed PhraseSearch runtime differs from the verified manifest.');
  }
  console.log(
    `[e2e] PhraseSearch runtime verified activeBuildId=${expected.activeBuildId} sourceFingerprint=${expected.sourceFingerprint}`,
  );
}

function loadBackendConnectionString(apiProject) {
  const configured = process.env.ConnectionStrings__QuranDashboardDb?.trim();
  if (configured) {
    return configured;
  }

  let output;
  try {
    output = execFileSync(
      'dotnet',
      ['user-secrets', 'list', '--json', '--project', apiProject],
      { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] },
    );
  } catch {
    throw new Error(
      'clone-local requires ConnectionStrings__QuranDashboardDb or the API user secret.',
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
    throw new Error('ConnectionStrings:QuranDashboardDb is missing from the API user secrets.');
  }
  return connectionString;
}

function formatConnectionString(connection) {
  return [
    `Host=${connection.host}`,
    `Port=${connection.port}`,
    `Database=${connection.database}`,
    `Username=${connection.username}`,
    `Password=${connection.password}`,
  ].join(';');
}

function connectionArguments(connection) {
  return [
    '--host',
    connection.host,
    '--port',
    connection.port,
    '--username',
    connection.username,
  ];
}

function postgresEnvironment(connection) {
  return {
    ...process.env,
    PGPASSWORD: connection.password,
    PGCONNECT_TIMEOUT: '10',
  };
}

async function waitForPostgres(connection) {
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    if (
      tryCommand(
        'pg_isready',
        [...connectionArguments(connection), '--dbname', connection.database],
        postgresEnvironment(connection),
      )
    ) {
      return;
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 250));
  }
  throw new Error('The artifact PostgreSQL container did not become ready within 30 seconds.');
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

function tryCommand(command, arguments_, environment) {
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

function once(action) {
  let called = false;
  return () => {
    if (called) {
      return;
    }
    called = true;
    action();
  };
}
