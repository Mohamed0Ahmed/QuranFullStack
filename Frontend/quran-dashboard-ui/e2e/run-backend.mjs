import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { writeFileSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname, isAbsolute, resolve } from 'node:path';

import { COMPACT_ARTIFACT_IDS } from './harness/artifact-contract.mjs';
import {
  provisionDatabaseRuntime,
  readPreparedDatabaseRuntime,
  removeDatabaseRuntimeState,
  writeDatabaseRuntimeState,
} from './harness/database-runtime.mjs';

const API_PROJECT = resolve(
  process.cwd(),
  '../../Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj',
);
const managementClientId = 'e2e-management-client';
const managementClientSecret = randomBytes(24).toString('base64url');
const managementAccessToken = randomBytes(24).toString('base64url');
const statefulExecution = process.env.E2E_DATABASE_MODE === 'persistent-stateful';

let backendProcess;
let databaseRuntime;
let managementServer;
let stopping = false;

process.once('SIGINT', () => stop('SIGINT'));
process.once('SIGTERM', () => stop('SIGTERM'));

try {
  if (process.env.E2E_SEALED_EXECUTION === '1' && process.env.E2E_PREPARED_DATABASE !== '1') {
    throw new Error('Sealed execution requires database preparation before application startup.');
  }
  databaseRuntime = statefulExecution
    ? persistentStatefulRuntime()
    : process.env.E2E_PREPARED_DATABASE === '1'
      ? readPreparedDatabaseRuntime()
      : await provisionDatabaseRuntime(API_PROJECT);
  const managementApi = await startManagementApiStub();
  managementServer = managementApi.server;

  if (!statefulExecution) {
    writeDatabaseRuntimeState(databaseRuntime);
  }
  console.log(
    statefulExecution
      ? `[e2e] database mode=persistent-stateful target=quran_dashboard_test profile=${process.env.Testing__DatabaseActivity__Profile}`
      : databaseRuntime.mode === 'artifact'
      ? `[e2e] database mode=artifact artifacts=${COMPACT_ARTIFACT_IDS.join(',')} evidence=canonical`
      : '[e2e] database mode=clone-local evidence=non-canonical',
  );

  const backendAssembly = process.env.E2E_BACKEND_ASSEMBLY;
  const backendArguments = backendAssembly
    ? [backendAssembly]
    : ['run', '--project', API_PROJECT, '--no-build', '--no-restore', '--no-launch-profile'];
  if (process.env.E2E_CONTROLLED_EXECUTION === '1') {
    backendArguments.push('--Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Warning');
  }
  backendProcess = spawn(
    'dotnet',
    backendArguments,
    {
      cwd: backendAssembly ? dirname(backendAssembly) : process.cwd(),
      env: {
        ...process.env,
        ConnectionStrings__QuranDashboardDb: databaseRuntime.connectionString,
        Auth__ManagementApi__Endpoint: managementApi.endpoint,
        Auth__ManagementApi__Resource: 'e2e-management-api',
        Auth__ManagementApi__AppId: managementClientId,
        Auth__ManagementApi__AppSecret: managementClientSecret,
      },
      stdio: 'inherit',
    },
  );
  if (statefulExecution) {
    writeApiProcessReceipt(backendProcess.pid);
  }
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
}

function finish(exitCode, message) {
  stopping = true;
  databaseRuntime?.cleanup();
  managementServer?.closeAllConnections();
  managementServer?.close();
  if (!statefulExecution && process.env.E2E_PREPARED_DATABASE !== '1') {
    removeDatabaseRuntimeState();
  }
  if (message) {
    console.error(message);
  }
  process.exit(exitCode);
}

function persistentStatefulRuntime() {
  const connectionString = process.env.ConnectionStrings__QuranDashboardTest?.trim();
  const profile = process.env.Testing__DatabaseActivity__Profile;
  if (!connectionString) {
    throw new Error('Stateful Playwright requires ConnectionStrings__QuranDashboardTest.');
  }
  if (!process.env.E2E_BACKEND_ASSEMBLY || !isAbsolute(process.env.E2E_BACKEND_ASSEMBLY)) {
    throw new Error('Stateful Playwright requires the absolute built Backend assembly path.');
  }
  if (!['ReadOnly', 'Mutable'].includes(profile)) {
    throw new Error('Stateful Playwright requires a ReadOnly or Mutable API activity profile.');
  }
  const expectedContext = profile === 'ReadOnly'
    ? process.env.QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT
    : process.env.QURAN_DASHBOARD_TEST_RUNTIME_WRITER_CONTEXT;
  if (expectedContext !== 'verified-v1') {
    throw new Error(`Stateful Playwright requires the verified TestRuntime ${profile} context.`);
  }
  if (
    profile === 'ReadOnly'
    && Object.keys(process.env).some((name) =>
      name.startsWith('Testing__DatabaseActivity__EnabledBackgroundActivities__'))
  ) {
    throw new Error('Stateful guarded-read Playwright cannot enable background activity.');
  }
  return {
    mode: 'persistent-stateful',
    connectionString,
    cleanup() {},
  };
}

function writeApiProcessReceipt(processId) {
  const path = process.env.E2E_API_PROCESS_RECEIPT;
  if (!path || !isAbsolute(path)) {
    throw new Error('Stateful Playwright requires an absolute API process receipt path.');
  }
  if (!Number.isInteger(processId) || processId < 1) {
    throw new Error('Stateful Playwright could not verify the spawned API process ID.');
  }
  try {
    writeFileSync(
      path,
      `${JSON.stringify({ schemaVersion: 1, processId, port: 5015 })}\n`,
      { encoding: 'utf8', mode: 0o600 },
    );
  } catch (error) {
    backendProcess.kill('SIGTERM');
    throw error;
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
