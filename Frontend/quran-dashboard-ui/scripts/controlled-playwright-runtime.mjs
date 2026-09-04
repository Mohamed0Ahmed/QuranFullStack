import { spawn } from 'node:child_process';
import {
  appendFileSync,
  lstatSync,
  readFileSync,
  readdirSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';

import {
  createControlledEnvironment,
  redactDiagnosticText,
  validateControlledProvisioningReceipt,
} from '../e2e/harness/controlled-execution-contract.mjs';
import {
  findFiles,
  controlledHarnessSourceFiles,
  sha256Files,
  sha256Path,
} from '../e2e/harness/provisioning-integrity.mjs';
import {
  APPROVED_DIAGNOSTIC_FILES,
  validateRetainedDiagnostic,
} from './structured-playwright-reporter.mjs';

export function loadControlledProvisioningReceipt(frontendRoot, repositoryRoot) {
  const receiptPath = resolve(frontendRoot, '.playwright/provisioning/controlled-receipt.json');
  let receipt;
  try {
    receipt = JSON.parse(readFileSync(receiptPath, 'utf8'));
  } catch {
    throw new Error('Run npm run e2e:provision before controlled Playwright execution.');
  }
  validateControlledProvisioningReceipt(receipt);

  const npmLock = resolve(frontendRoot, 'package-lock.json');
  const nugetLocks = findFiles(resolve(repositoryRoot, 'Backend'), 'packages.lock.json');
  const expectedInputs = {
    harnessSourceSha256: sha256Files(controlledHarnessSourceFiles(frontendRoot), repositoryRoot),
    npmLockSha256: sha256Files([npmLock], repositoryRoot),
    nugetLocksSha256: sha256Files(nugetLocks, repositoryRoot),
  };
  for (const [name, value] of Object.entries(expectedInputs)) {
    if (receipt.inputs[name] !== value) {
      throw new Error(`Controlled provisioning input ${name} is stale; provision again.`);
    }
  }
  for (const [name, path] of Object.entries(receipt.outputs)) {
    if (!isFileOrDirectory(path)) {
      throw new Error(`Controlled provisioning output ${name} is missing; provision again.`);
    }
    if (sha256Path(path) !== receipt.outputSha256[name]) {
      throw new Error(`Controlled provisioning output ${name} has changed; provision again.`);
    }
  }
  if ((statSync(receipt.outputs.tlsPrivateKey).mode & 0o077) !== 0) {
    throw new Error('The controlled TLS private key must not be accessible to group or other users.');
  }

  const browserLock = JSON.parse(
    readFileSync(resolve(frontendRoot, 'node_modules/playwright-core/browsers.json'), 'utf8'),
  );
  const chromium = browserLock.browsers?.find((browser) => browser.name === 'chromium');
  if (
    chromium?.revision !== receipt.inputs.chromiumRevision
    || chromium?.browserVersion !== receipt.inputs.chromiumVersion
  ) {
    throw new Error('The installed Playwright Chromium build differs from controlled provisioning.');
  }
  return receipt;
}

export function createControlledPlaywrightEnvironment(source, receipt, paths) {
  return createControlledEnvironment(source, {
    backendAssembly: resolve(receipt.outputs.backendOutput, 'QuranDashboard.Api.dll'),
    chromiumExecutable: receipt.outputs.chromiumExecutable,
    egressGuard: receipt.outputs.egressGuard,
    evidenceDirectory: paths.evidenceDirectory,
    frontendBuild: receipt.outputs.frontendBuild,
    homeDirectory: paths.homeDirectory,
    playwrightOutputDirectory: paths.playwrightOutputDirectory,
    testRuntimeAssembly: resolve(
      receipt.outputs.testRuntimeOutput,
      'QuranDashboard.TestRuntime.dll',
    ),
    tlsCertificate: receipt.outputs.tlsCertificate,
    tlsPrivateKey: receipt.outputs.tlsPrivateKey,
  });
}

export function runWithSanitizedOutput(
  command,
  arguments_,
  { cwd, environment, logPath, secretValues, onSpawn },
) {
  return new Promise((resolveRun, rejectRun) => {
    const child = spawn(command, arguments_, {
      cwd,
      detached: true,
      env: environment,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    onSpawn?.(child);
    const buffers = new Map([[child.stdout, ''], [child.stderr, '']]);
    for (const stream of buffers.keys()) {
      stream.setEncoding('utf8');
      stream.on('data', (chunk) => {
        const lines = `${buffers.get(stream)}${chunk}`.split('\n');
        buffers.set(stream, lines.pop() ?? '');
        for (const line of lines) recordSanitizedLine(line, logPath, secretValues);
      });
    }
    child.once('error', rejectRun);
    child.once('close', (code, signal) => {
      for (const pending of buffers.values()) {
        if (pending) recordSanitizedLine(pending, logPath, secretValues);
      }
      resolveRun({ child, exitCode: code ?? (signal ? 1 : 0) });
    });
  });
}

export function validatePlaywrightChildResult(result, expected) {
  if (!result || result.schemaVersion !== 1 || !['passed', 'failed', 'timedout', 'interrupted'].includes(result.status)) {
    throw new Error('Playwright child returned an invalid structured result.');
  }
  if (result.runId !== expected.runId) {
    throw new Error('Playwright child result does not match the expected run ID.');
  }
  if (result.declaredTestCount !== expected.declaredTestCount) {
    throw new Error('Playwright child result has an unexpected declared test count.');
  }
  if (
    !Array.isArray(result.tests)
    || !result.counts
    || typeof result.counts !== 'object'
    || Array.isArray(result.counts)
  ) {
    throw new Error('Playwright child result is missing sanitized test evidence.');
  }
  return result;
}

export function inspectRetainedEvidence(directory, secretValues) {
  let inventory = listEvidence(directory);
  for (const symlink of inventory.symlinks) rmSync(resolve(directory, symlink), { force: true });
  const removedRawFiles = inventory.files.filter((file) =>
    /\.(?:bak|dump|har|html|key|md|pem|sql|trace|zip)$/i.test(file),
  );
  for (const file of removedRawFiles) rmSync(resolve(directory, file), { force: true });

  inventory = listEvidence(directory);
  const rewrittenTextFiles = [];
  for (const file of inventory.files.filter((candidate) => /\.(?:json|log|txt)$/i.test(candidate))) {
    const path = resolve(directory, file);
    const content = readFileSync(path, 'utf8');
    const sanitized = file.endsWith('.json')
      ? sanitizeJsonText(content, secretValues)
      : redactDiagnosticText(content, secretValues);
    if (sanitized !== content) {
      writeFileSync(path, sanitized, { encoding: 'utf8', mode: 0o600 });
      rewrittenTextFiles.push(file);
    }
  }

  const invalidDiagnosticFiles = [];
  for (const file of inventory.files.filter((candidate) => candidate.startsWith('diagnostics/'))) {
    const parts = file.split('/');
    if (parts.length !== 3 || !parts[1] || !APPROVED_DIAGNOSTIC_FILES.includes(parts[2])) {
      invalidDiagnosticFiles.push(file);
      continue;
    }
    try {
      validateRetainedDiagnostic(parts[2], readFileSync(resolve(directory, file)));
    } catch {
      invalidDiagnosticFiles.push(file);
    }
  }
  for (const file of invalidDiagnosticFiles) rmSync(resolve(directory, file), { force: true });

  const approvedRootFiles = new Set([
    'application.log',
    'playwright-results.json',
    'sanitized-trace.json',
  ]);
  const unexpectedFiles = inventory.files.filter((file) =>
    !approvedRootFiles.has(file) && !file.startsWith('diagnostics/'),
  );
  for (const file of unexpectedFiles) rmSync(resolve(directory, file), { force: true });

  let unsafeScreenshot = false;
  try {
    const playwright = JSON.parse(readFileSync(resolve(directory, 'playwright-results.json'), 'utf8'));
    unsafeScreenshot = playwright.tests?.some((test) => test.attachments?.some(
      (attachment) => attachment.contentType === 'image/png'
        && attachment.name !== 'sanitized-screenshot',
    )) ?? false;
  } catch {
    // A startup failure may happen before Playwright writes its result.
  }

  return {
    status: inventory.symlinks.length === 0
      && invalidDiagnosticFiles.length === 0
      && unexpectedFiles.length === 0
      && !unsafeScreenshot
      ? 'passed'
      : 'failed',
    invalidDiagnosticFiles,
    removedRawFiles,
    rewrittenTextFiles,
    symlinks: inventory.symlinks,
    unexpectedFiles,
    unsafeScreenshot,
  };
}

function sanitizeJsonText(content, secretValues) {
  try {
    const parsed = JSON.parse(content);
    const sanitizeValue = (value) => {
      if (typeof value === 'string') return redactDiagnosticText(value, secretValues);
      if (Array.isArray(value)) return value.map(sanitizeValue);
      if (value && typeof value === 'object') {
        return Object.fromEntries(
          Object.entries(value).map(([key, nested]) => [key, sanitizeValue(nested)]),
        );
      }
      return value;
    };
    return `${JSON.stringify(sanitizeValue(parsed), null, 2)}\n`;
  } catch {
    return redactDiagnosticText(content, secretValues);
  }
}

export function writeJson(path, value) {
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`, {
    encoding: 'utf8',
    mode: 0o600,
  });
}

export function sanitizeError(error, secretValues) {
  return redactDiagnosticText(
    error instanceof Error ? error.stack ?? error.message : 'Controlled Playwright execution failed.',
    secretValues,
  );
}

function recordSanitizedLine(line, logPath, secretValues) {
  const sanitized = redactDiagnosticText(line, secretValues);
  appendFileSync(logPath, `${sanitized}\n`, { encoding: 'utf8', mode: 0o600 });
  process.stdout.write(`${sanitized}\n`);
}

function listEvidence(directory, root = directory) {
  const files = [];
  const symlinks = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    const relativePath = relative(root, path);
    if (entry.isSymbolicLink() || lstatSync(path).isSymbolicLink()) symlinks.push(relativePath);
    else if (entry.isDirectory()) {
      const nested = listEvidence(path, root);
      files.push(...nested.files);
      symlinks.push(...nested.symlinks);
    } else if (entry.isFile()) files.push(relativePath);
  }
  return { files: files.sort(), symlinks: symlinks.sort() };
}

function isFileOrDirectory(path) {
  try {
    const details = statSync(path);
    return details.isFile() || details.isDirectory();
  } catch {
    return false;
  }
}
