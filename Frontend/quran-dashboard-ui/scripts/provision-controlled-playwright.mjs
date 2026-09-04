import { execFileSync } from 'node:child_process';
import { chmodSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  findFiles,
  findFilesByExtension,
  controlledHarnessSourceFiles,
  sha256Files,
  sha256Path,
} from '../e2e/harness/provisioning-integrity.mjs';

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const backendRoot = resolve(repositoryRoot, 'Backend');
const runtimeRoot = resolve(frontendRoot, '.playwright/provisioning');
const receiptPath = resolve(runtimeRoot, 'controlled-receipt.json');
const tlsCertificate = resolve(runtimeRoot, 'localhost.pem');
const tlsPrivateKey = resolve(runtimeRoot, 'localhost-key.pem');
const egressGuard = resolve(runtimeRoot, 'egress-guard.so');
const npmLockPath = resolve(frontendRoot, 'package-lock.json');
const backendOutput = resolve(backendRoot, 'api/QuranDashboard.Api/bin/Debug/net10.0');
const testRuntimeOutput = resolve(
  backendRoot,
  'tools/QuranDashboard.TestRuntime/bin/Debug/net10.0',
);
const frontendBuild = resolve(frontendRoot, 'dist/quran-dashboard-ui/browser');

mkdirSync(runtimeRoot, { recursive: true });
const startedAt = new Date();
const phases = [];
let receipt;

try {
  runPhase('dependencyProvisioning', () => {
    run('npm', ['ci'], frontendRoot);
    run('dotnet', [
      'restore',
      'QuranDashboard.sln',
      '--locked-mode',
      '--disable-parallel',
      '-m:1',
      '-p:BuildInParallel=false',
      '-p:RestoreDisableParallel=true',
    ], backendRoot);
  });

  const playwright = await import('playwright');
  const chromiumLock = readChromiumLock();
  runPhase('chromiumProvisioning', () => {
    run(resolve(frontendRoot, 'node_modules/.bin/playwright'), ['install', 'chromium'], frontendRoot);
    requireOutput(playwright.chromium.executablePath(), 'the exact Playwright Chromium executable');
  });

  runPhase('certificateProvisioning', () => {
    run('openssl', [
      'req',
      '-x509',
      '-newkey',
      'rsa:2048',
      '-sha256',
      '-nodes',
      '-days',
      '1',
      '-subj',
      '/CN=localhost',
      '-addext',
      'subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1',
      '-keyout',
      tlsPrivateKey,
      '-out',
      tlsCertificate,
    ], frontendRoot, 'ignore');
    chmodSync(tlsPrivateKey, 0o600);
    chmodSync(tlsCertificate, 0o644);
  });

  runPhase('buildProvisioning', () => {
    run('cc', [
      '-shared',
      '-fPIC',
      '-O2',
      '-Wall',
      '-Wextra',
      '-Werror',
      '-o',
      egressGuard,
      'e2e/harness/egress-guard.c',
    ], frontendRoot);
    run(process.execPath, ['scripts/verify-egress-guard-runtime.mjs'], frontendRoot, 'inherit', {
      ...process.env,
      LD_PRELOAD: egressGuard,
    });
    run('dotnet', [
      'build',
      'QuranDashboard.sln',
      '--no-restore',
      '--configuration',
      'Debug',
      '--disable-build-servers',
      '-m:1',
      '-p:BuildInParallel=false',
    ], backendRoot);
    run('npm', ['run', 'build', '--', '--configuration', 'development'], frontendRoot);
  });

  const nugetLocks = findFiles(backendRoot, 'packages.lock.json');
  const unlockedProjects = findFilesByExtension(backendRoot, '.csproj').filter(
    (project) => !nugetLocks.includes(resolve(dirname(project), 'packages.lock.json')),
  );
  if (unlockedProjects.length > 0) {
    throw new Error(
      `Locked NuGet provisioning found projects without packages.lock.json: ${unlockedProjects.join(', ')}`,
    );
  }

  receipt = {
    schemaVersion: 2,
    status: 'passed',
    startedAt: startedAt.toISOString(),
    completedAt: new Date().toISOString(),
    durationMs: Date.now() - startedAt.getTime(),
    inputs: {
      chromiumRevision: chromiumLock.revision,
      chromiumVersion: chromiumLock.browserVersion,
      harnessSourceSha256: sha256Files(controlledHarnessSourceFiles(frontendRoot), repositoryRoot),
      npmLockSha256: sha256Files([npmLockPath], repositoryRoot),
      nugetLocksSha256: sha256Files(nugetLocks, repositoryRoot),
    },
    outputs: {
      backendOutput,
      chromiumExecutable: playwright.chromium.executablePath(),
      egressGuard,
      frontendBuild,
      testRuntimeOutput,
      tlsCertificate,
      tlsPrivateKey,
    },
    phases,
  };
  for (const output of Object.values(receipt.outputs)) {
    requireOutput(output, 'a provisioned execution output');
  }
  receipt.outputSha256 = Object.fromEntries(
    Object.entries(receipt.outputs).map(([name, path]) => [name, sha256Path(path)]),
  );
} catch (error) {
  receipt = {
    schemaVersion: 2,
    status: 'failed',
    startedAt: startedAt.toISOString(),
    completedAt: new Date().toISOString(),
    durationMs: Date.now() - startedAt.getTime(),
    phases,
    error: error instanceof Error ? error.message : 'Controlled provisioning failed.',
  };
}

writeFileSync(receiptPath, `${JSON.stringify(receipt, null, 2)}\n`, {
  encoding: 'utf8',
  mode: 0o600,
});

if (receipt.status !== 'passed') {
  console.error(receipt.error);
  process.exit(1);
}

console.log(
  `[e2e] controlled provisioning passed durationMs=${receipt.durationMs} chromiumRevision=${receipt.inputs.chromiumRevision}`,
);

function runPhase(name, action) {
  const phaseStartedAt = Date.now();
  try {
    action();
    phases.push({ name, status: 'passed', durationMs: Date.now() - phaseStartedAt });
  } catch (error) {
    phases.push({ name, status: 'failed', durationMs: Date.now() - phaseStartedAt });
    throw error;
  }
}

function run(command, arguments_, cwd, stdio = 'inherit', environment = process.env) {
  execFileSync(command, arguments_, { cwd, env: environment, stdio });
}

function readChromiumLock() {
  const browserLockPath = resolve(frontendRoot, 'node_modules/playwright-core/browsers.json');
  const browserLock = JSON.parse(readFileSync(browserLockPath, 'utf8'));
  const matches = browserLock.browsers?.filter((browser) => browser.name === 'chromium') ?? [];
  if (
    matches.length !== 1
    || typeof matches[0].revision !== 'string'
    || typeof matches[0].browserVersion !== 'string'
  ) {
    throw new Error('The installed Playwright package does not declare one exact Chromium revision.');
  }
  return matches[0];
}

function requireOutput(path, semanticName) {
  let details;
  try {
    details = statSync(path);
  } catch {
    throw new Error(`Provisioning did not create ${semanticName}: ${path}`);
  }
  if (!details.isFile() && !details.isDirectory()) {
    throw new Error(`Provisioning created an unsupported ${semanticName}: ${path}`);
  }
}
