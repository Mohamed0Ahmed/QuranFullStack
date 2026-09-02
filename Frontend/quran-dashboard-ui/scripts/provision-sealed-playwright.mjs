import { execFileSync } from 'node:child_process';
import {
  chmodSync,
  mkdirSync,
  readFileSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { COMPACT_ARTIFACT_IDS } from '../e2e/harness/artifact-contract.mjs';
import {
  findFiles,
  findFilesByExtension,
  harnessSourceFiles,
  sha256Files,
  sha256Path,
} from '../e2e/harness/provisioning-integrity.mjs';

const FRONTEND_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const REPOSITORY_ROOT = resolve(FRONTEND_ROOT, '../..');
const BACKEND_ROOT = resolve(REPOSITORY_ROOT, 'Backend');
const RUNTIME_ROOT = resolve(FRONTEND_ROOT, '.playwright/provisioning');
const RECEIPT_PATH = resolve(RUNTIME_ROOT, 'receipt.json');
const TLS_CERTIFICATE = resolve(RUNTIME_ROOT, 'localhost.pem');
const TLS_PRIVATE_KEY = resolve(RUNTIME_ROOT, 'localhost-key.pem');
const EGRESS_GUARD = resolve(RUNTIME_ROOT, 'egress-guard.so');
const ARTIFACT_LOCK_PATH = resolve(REPOSITORY_ROOT, 'test-artifacts.lock.json');
const NPM_LOCK_PATH = resolve(FRONTEND_ROOT, 'package-lock.json');
const API_OUTPUT = resolve(
  BACKEND_ROOT,
  'api/QuranDashboard.Api/bin/Debug/net10.0',
);
const ARTIFACT_VERIFIER_OUTPUT = resolve(
  BACKEND_ROOT,
  'tools/QuranDashboard.TestArtifacts/bin/Debug/net10.0',
);
const FRONTEND_BUILD = resolve(FRONTEND_ROOT, 'dist/quran-dashboard-ui/browser');

mkdirSync(RUNTIME_ROOT, { recursive: true });
const startedAt = new Date();
const phases = [];
let receipt;

try {
  runPhase('dependencyProvisioning', () => {
    run('npm', ['ci'], FRONTEND_ROOT);
    run(
      'dotnet',
      [
        'restore',
        'QuranDashboard.sln',
        '--locked-mode',
        '--disable-parallel',
        '-m:1',
        '-p:BuildInParallel=false',
        '-p:RestoreDisableParallel=true',
      ],
      BACKEND_ROOT,
    );
  });

  const playwright = await import('playwright');
  const chromiumLock = readChromiumLock();
  runPhase('chromiumProvisioning', () => {
    run(resolve(FRONTEND_ROOT, 'node_modules/.bin/playwright'), ['install', 'chromium'], FRONTEND_ROOT);
    requireFile(playwright.chromium.executablePath(), 'the exact Playwright Chromium executable');
  });

  const postgresqlImage = readPostgresqlImage();
  runPhase('postgresqlProvisioning', () => {
    run('docker', ['pull', postgresqlImage], REPOSITORY_ROOT);
    run('docker', ['image', 'inspect', postgresqlImage], REPOSITORY_ROOT, 'ignore');
  });

  runPhase('artifactProvisioning', () => {
    const artifactProject = 'tools/QuranDashboard.TestArtifacts/QuranDashboard.TestArtifacts.csproj';
    run(
      'dotnet',
      [
        'build',
        artifactProject,
        '--no-restore',
        '--disable-build-servers',
        '-m:1',
        '-p:BuildInParallel=false',
      ],
      BACKEND_ROOT,
    );
    for (const artifactId of COMPACT_ARTIFACT_IDS) {
      run(
        'dotnet',
        [
          'tools/QuranDashboard.TestArtifacts/bin/Debug/net10.0/QuranDashboard.TestArtifacts.dll',
          'verify',
          '--artifact',
          artifactId,
          '--root',
          REPOSITORY_ROOT,
        ],
        BACKEND_ROOT,
      );
    }
  });

  runPhase('certificateProvisioning', () => {
    run(
      'openssl',
      [
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
        TLS_PRIVATE_KEY,
        '-out',
        TLS_CERTIFICATE,
      ],
      FRONTEND_ROOT,
      'ignore',
    );
    chmodSync(TLS_PRIVATE_KEY, 0o600);
    chmodSync(TLS_CERTIFICATE, 0o644);
  });

  runPhase('buildProvisioning', () => {
    run(
      'cc',
      [
        '-shared',
        '-fPIC',
        '-O2',
        '-Wall',
        '-Wextra',
        '-Werror',
        '-o',
        EGRESS_GUARD,
        'e2e/harness/egress-guard.c',
      ],
      FRONTEND_ROOT,
    );
    run(
      process.execPath,
      ['scripts/verify-egress-guard-runtime.mjs'],
      FRONTEND_ROOT,
      'inherit',
      {
        ...process.env,
        LD_PRELOAD: EGRESS_GUARD,
        QDB_E2E_ALLOWED_IPV4: '192.0.2.2',
      },
    );
    run(
      'dotnet',
      [
        'build',
        'QuranDashboard.sln',
        '--no-restore',
        '--configuration',
        'Debug',
        '--disable-build-servers',
        '-m:1',
        '-p:BuildInParallel=false',
      ],
      BACKEND_ROOT,
    );
    run('npm', ['run', 'build', '--', '--configuration', 'development'], FRONTEND_ROOT);
  });

  const nugetLocks = findFiles(BACKEND_ROOT, 'packages.lock.json');
  const backendProjects = findFilesByExtension(BACKEND_ROOT, '.csproj');
  const unlockedProjects = backendProjects.filter(
    (project) => !nugetLocks.includes(resolve(dirname(project), 'packages.lock.json')),
  );
  if (unlockedProjects.length > 0) {
    throw new Error(
      `Locked NuGet provisioning found projects without packages.lock.json: ${unlockedProjects.join(', ')}`,
    );
  }

  receipt = {
    schemaVersion: 1,
    status: 'passed',
    startedAt: startedAt.toISOString(),
    completedAt: new Date().toISOString(),
    durationMs: Date.now() - startedAt.getTime(),
    inputs: {
      npmLockSha256: sha256Files([NPM_LOCK_PATH], REPOSITORY_ROOT),
      nugetLocksSha256: sha256Files(nugetLocks, REPOSITORY_ROOT),
      artifactLockSha256: sha256Files([ARTIFACT_LOCK_PATH], REPOSITORY_ROOT),
      harnessSourceSha256: sha256Files(harnessSourceFiles(FRONTEND_ROOT), REPOSITORY_ROOT),
      chromiumRevision: chromiumLock.revision,
      chromiumVersion: chromiumLock.browserVersion,
      postgresqlImage,
    },
    outputs: {
      artifactVerifierOutput: ARTIFACT_VERIFIER_OUTPUT,
      chromiumExecutable: playwright.chromium.executablePath(),
      frontendBuild: FRONTEND_BUILD,
      backendOutput: API_OUTPUT,
      tlsCertificate: TLS_CERTIFICATE,
      tlsPrivateKey: TLS_PRIVATE_KEY,
      egressGuard: EGRESS_GUARD,
    },
    phases,
  };
  for (const output of Object.values(receipt.outputs)) {
    requireFile(output, 'a provisioned execution output');
  }
  receipt.outputSha256 = Object.fromEntries(
    Object.entries(receipt.outputs).map(([name, path]) => [name, sha256Path(path)]),
  );
} catch (error) {
  receipt = {
    schemaVersion: 1,
    status: 'failed',
    startedAt: startedAt.toISOString(),
    completedAt: new Date().toISOString(),
    durationMs: Date.now() - startedAt.getTime(),
    phases,
    error: error instanceof Error ? error.message : 'Provisioning failed.',
  };
}

writeFileSync(RECEIPT_PATH, `${JSON.stringify(receipt, null, 2)}\n`, {
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
  execFileSync(command, arguments_, {
    cwd,
    env: environment,
    stdio,
  });
}

function readChromiumLock() {
  const browserLockPath = resolve(FRONTEND_ROOT, 'node_modules/playwright-core/browsers.json');
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

function readPostgresqlImage() {
  const lock = JSON.parse(readFileSync(ARTIFACT_LOCK_PATH, 'utf8'));
  const artifacts = COMPACT_ARTIFACT_IDS.map((artifactId) => {
    const matches = lock.artifacts?.filter((artifact) => artifact.id === artifactId) ?? [];
    if (matches.length !== 1) {
      throw new Error(`Expected one locked compact artifact named ${artifactId}.`);
    }
    return matches[0];
  });
  const digests = new Set(artifacts.map((artifact) => artifact.postgresql?.containerDigest));
  const [digest] = digests;
  if (digests.size !== 1 || !/^sha256:[a-f0-9]{64}$/.test(digest)) {
    throw new Error('Composable compact artifacts must lock one PostgreSQL image digest.');
  }
  return `postgres@${digest}`;
}

function requireFile(path, semanticName) {
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
