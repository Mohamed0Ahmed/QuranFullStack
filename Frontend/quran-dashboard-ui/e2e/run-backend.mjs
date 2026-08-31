import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:http';
import { dirname, resolve } from 'node:path';

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
  databaseRuntime = process.env.E2E_PREPARED_DATABASE === '1'
    ? readPreparedDatabaseRuntime()
    : await provisionDatabaseRuntime(API_PROJECT);
  const managementApi = await startManagementApiStub();
  managementServer = managementApi.server;

  writeDatabaseRuntimeState(databaseRuntime);
  console.log(
    databaseRuntime.mode === 'artifact'
      ? `[e2e] database mode=artifact artifacts=${COMPACT_ARTIFACT_IDS.join(',')} evidence=canonical`
      : '[e2e] database mode=clone-local evidence=non-canonical',
  );

  const backendAssembly = process.env.E2E_BACKEND_ASSEMBLY;
  backendProcess = spawn(
    'dotnet',
    backendAssembly
      ? [backendAssembly]
      : ['run', '--project', API_PROJECT, '--no-build', '--no-restore', '--no-launch-profile'],
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
  if (process.env.E2E_PREPARED_DATABASE !== '1') {
    removeDatabaseRuntimeState();
  }
  if (message) {
    console.error(message);
  }
  process.exit(exitCode);
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
