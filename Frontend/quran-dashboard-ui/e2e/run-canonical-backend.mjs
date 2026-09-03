import { spawn } from 'node:child_process';
import { dirname, resolve } from 'node:path';

const API_PROJECT = resolve(
  process.cwd(),
  '../../Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj',
);
const connectionString = process.env.ConnectionStrings__QuranDashboardTest?.trim();

if (process.env.QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT !== 'verified-v1') {
  throw new Error('Canonical Playwright requires the verified TestRuntime reader context.');
}
if (!connectionString) {
  throw new Error('Canonical Playwright requires ConnectionStrings__QuranDashboardTest.');
}
if (process.env.Testing__DatabaseActivity__Profile !== 'ReadOnly') {
  throw new Error('Canonical Playwright requires the Testing ReadOnly API profile.');
}
if (
  Object.keys(process.env).some((name) =>
    name.startsWith('Testing__DatabaseActivity__EnabledBackgroundActivities__'),
  )
) {
  throw new Error('Canonical Playwright cannot enable database background activity.');
}

const backendAssembly = process.env.E2E_BACKEND_ASSEMBLY;
const backendProcess = spawn(
  'dotnet',
  backendAssembly
    ? [backendAssembly]
    : ['run', '--project', API_PROJECT, '--no-build', '--no-restore', '--no-launch-profile'],
  {
    cwd: backendAssembly ? dirname(backendAssembly) : process.cwd(),
    env: {
      ...process.env,
      ConnectionStrings__QuranDashboardDb: connectionString,
    },
    stdio: 'inherit',
  },
);

let stopping = false;
process.once('SIGINT', () => stop('SIGINT'));
process.once('SIGTERM', () => stop('SIGTERM'));
backendProcess.once('error', () => finish(1, 'The canonical E2E backend could not be started.'));
backendProcess.once('exit', (code) => {
  if (!stopping) finish(code ?? 1);
});

console.log('[e2e] database mode=persistent-read-only target=quran_dashboard_test evidence=canonical');

function stop(signal) {
  if (stopping) return;
  stopping = true;
  if (backendProcess.exitCode !== null || backendProcess.signalCode !== null) {
    finish(0);
    return;
  }

  const forcedShutdown = setTimeout(() => backendProcess.kill('SIGKILL'), 15_000);
  backendProcess.once('exit', () => {
    clearTimeout(forcedShutdown);
    finish(0);
  });
  backendProcess.kill(signal);
}

function finish(exitCode, message) {
  stopping = true;
  if (message) console.error(message);
  process.exit(exitCode);
}
