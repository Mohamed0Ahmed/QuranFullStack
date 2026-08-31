import { spawn } from 'node:child_process';
import { mkdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { basename, dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  evaluateDependencyAdvisories,
  loadDependencyAdvisoryContract,
  loadNugetLocks,
  nugetReportHasFindings,
} from './dependency-advisory-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const POLICY_PATH = resolve(REPOSITORY_ROOT, 'dependency-advisory-policy.json');
const WAIVERS_PATH = resolve(REPOSITORY_ROOT, 'dependency-advisory-waivers.json');
const MAX_CAPTURE_BYTES = 20 * 1024 * 1024;

let options;
try {
  options = parseArguments(process.argv.slice(2));
} catch (error) {
  console.error(error.message);
  printUsage();
  process.exit(2);
}

let contract;
try {
  contract = loadDependencyAdvisoryContract({
    policyPath: POLICY_PATH,
    repositoryRoot: REPOSITORY_ROOT,
    waiversPath: WAIVERS_PATH,
  });
} catch (error) {
  console.error(error.message);
  process.exit(2);
}

if (options.trigger === 'nightly') {
  writeFileSync(2, 'nightly is excluded from dependency advisory evaluation.\n');
  process.exit(2);
}
if (!contract.policy.triggers.some(({ id }) => id === options.trigger)) {
  console.error(`Unknown dependency advisory trigger: ${options.trigger}`);
  printUsage();
  process.exit(2);
}

const scans = buildScans(contract.policy);
if (options.dryRun) {
  writeFileSync(1, `${JSON.stringify({
    policyId: contract.policy.id,
    trigger: options.trigger,
    excludedLanes: contract.policy.excludedLanes,
    resultsDirectory: options.resultsDirectory ?? '<generated>',
    scans: scans.map((scan) => ({
      id: scan.id,
      cwd: relative(REPOSITORY_ROOT, scan.cwd) || '.',
      executable: scan.executable,
      arguments: scan.arguments,
      condition: scan.condition ?? 'always',
    })),
    resultFile: contract.policy.evidence.resultFile,
  }, null, 2)}\n`);
  process.exit(0);
}

const evaluatedAt = new Date().toISOString();
const resultsDirectory = options.resultsDirectory ?? defaultResultsDirectory(options.trigger);
mkdirSync(resultsDirectory, { recursive: true });

const rawReports = new Map();
const scanErrors = [];
for (const scan of scans) {
  if (scan.condition === 'nuget-findings' && !nugetReportHasFindings(rawReports.get('nuget.json'))) {
    const skippedReport = {
      version: 1,
      projects: [],
      skipped: true,
      reason: 'No NuGet advisory finding requires remediation sizing.',
    };
    rawReports.set(scan.reportName, skippedReport);
    writeJsonAtomically(resolve(resultsDirectory, scan.reportName), skippedReport);
    console.log(`[dependency-advisory] ${scan.id} skipped reason=no-nuget-findings`);
    continue;
  }
  console.log(`[dependency-advisory] ${scan.id} started`);
  const result = await runCommand(scan);
  if (!scan.acceptableExitCodes.includes(result.exitCode)) {
    scanErrors.push({
      scan: scan.id,
      exitCode: result.exitCode,
      signal: result.signal,
      error: sanitizeDiagnostic(result.error ?? result.stderr ?? 'scan failed without diagnostics'),
    });
    break;
  }
  if (scan.reportName) {
    try {
      const report = JSON.parse(result.stdout);
      rawReports.set(scan.reportName, report);
      writeJsonAtomically(resolve(resultsDirectory, scan.reportName), report);
    } catch (error) {
      scanErrors.push({
        scan: scan.id,
        exitCode: result.exitCode,
        signal: result.signal,
        error: `scan did not return valid JSON: ${sanitizeDiagnostic(error.message)}`,
      });
      break;
    }
  }
  console.log(`[dependency-advisory] ${scan.id} completed exitCode=${result.exitCode}`);
}

const resultPath = resolve(resultsDirectory, contract.policy.evidence.resultFile);
if (scanErrors.length > 0) {
  const failedEvaluation = createBlockedEvaluation({ evaluatedAt, scanErrors });
  writeJsonAtomically(resultPath, failedEvaluation);
  console.error(
    `[dependency-advisory] status=blocked trigger=${options.trigger} scanErrors=${scanErrors.length} result=${resultPath}`,
  );
  process.exit(1);
}

let evaluation;
try {
  evaluation = evaluateDependencyAdvisories({
    evaluatedAt,
    nugetLocks: loadNugetLocks(contract.policy, REPOSITORY_ROOT),
    nugetOutdatedReport: rawReports.get('nuget-outdated.json'),
    nugetReport: rawReports.get('nuget.json'),
    npmAllReport: rawReports.get('npm-all.json'),
    npmPackageLock: JSON.parse(readFileSync(
      resolve(REPOSITORY_ROOT, contract.policy.ecosystems.npm.lockfile),
      'utf8',
    )),
    npmProductionReport: rawReports.get('npm-production.json'),
    policy: contract.policy,
    repositoryRoot: REPOSITORY_ROOT,
    trigger: options.trigger,
    waiversDocument: contract.waiversDocument,
  });
} catch (error) {
  evaluation = createBlockedEvaluation({
    evaluatedAt,
    scanErrors: [{ scan: 'evaluation', error: sanitizeDiagnostic(error.message) }],
  });
}

writeJsonAtomically(resultPath, evaluation);
console.log([
  `[dependency-advisory] status=${evaluation.status}`,
  `trigger=${options.trigger}`,
  `production=${evaluation.summary.production}`,
  `development=${evaluation.summary.development}`,
  `blocking=${evaluation.summary.blocking}`,
  `result=${resultPath}`,
].join(' '));
process.exitCode = evaluation.status === 'blocked' ? 1 : 0;

function parseArguments(arguments_) {
  const parsed = {
    dryRun: false,
    resultsDirectory: null,
    trigger: '',
  };
  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === '--trigger') {
      parsed.trigger = requireValue(arguments_, ++index, '--trigger');
    } else if (argument === '--results-dir') {
      parsed.resultsDirectory = resolve(process.cwd(), requireValue(arguments_, ++index, '--results-dir'));
    } else if (argument === '--dry-run') {
      parsed.dryRun = true;
    } else if (argument === '--help' || argument === '-h') {
      printUsage();
      process.exit(0);
    } else {
      throw new Error(`Unknown argument: ${argument}`);
    }
  }
  if (!parsed.trigger) {
    throw new Error('--trigger is required.');
  }
  return parsed;
}

function requireValue(arguments_, index, option) {
  const value = arguments_[index];
  if (!value || value.startsWith('--')) {
    throw new Error(`${option} requires a value.`);
  }
  return value;
}

function printUsage() {
  console.log(`Usage:
  node scripts/run-dependency-advisory-evaluation.mjs --trigger weekly [--results-dir PATH]
  node scripts/run-dependency-advisory-evaluation.mjs --trigger lockfile-change [--results-dir PATH]
  node scripts/run-dependency-advisory-evaluation.mjs --trigger release [--results-dir PATH]
  node scripts/run-dependency-advisory-evaluation.mjs --trigger TRIGGER --dry-run`);
}

function createBlockedEvaluation({ evaluatedAt: blockedAt, scanErrors: blockedErrors }) {
  return {
    schemaVersion: 1,
    policyId: contract.policy.id,
    trigger: options.trigger,
    evaluatedAt: blockedAt,
    status: 'blocked',
    summary: {
      total: 0,
      production: 0,
      development: 0,
      highCriticalProduction: 0,
      blocking: blockedErrors.length,
    },
    findings: [],
    blockingFindings: [],
    expiredWaivers: [],
    scanErrors: blockedErrors,
  };
}

function buildScans(policy) {
  const solutionPath = resolve(REPOSITORY_ROOT, policy.ecosystems.nuget.solution);
  const backendDirectory = dirname(solutionPath);
  const solutionName = basename(solutionPath);
  const npmDirectory = resolve(REPOSITORY_ROOT, policy.ecosystems.npm.directory);
  return [
    {
      id: 'locked-nuget-restore',
      cwd: backendDirectory,
      executable: 'dotnet',
      arguments: [
        'restore',
        solutionName,
        '--locked-mode',
        '--disable-parallel',
        '-m:1',
        '-p:BuildInParallel=false',
        '-p:RestoreDisableParallel=true',
      ],
      acceptableExitCodes: [0],
      reportName: null,
    },
    {
      id: 'nuget-advisories',
      cwd: backendDirectory,
      executable: 'dotnet',
      arguments: [
        'list',
        solutionName,
        'package',
        '--vulnerable',
        '--include-transitive',
        '--format',
        'json',
        '--output-version',
        '1',
        '--no-restore',
      ],
      acceptableExitCodes: [0],
      reportName: 'nuget.json',
    },
    {
      id: 'nuget-upgrade-candidates',
      cwd: backendDirectory,
      executable: 'dotnet',
      arguments: [
        'list',
        solutionName,
        'package',
        '--outdated',
        '--include-transitive',
        '--format',
        'json',
        '--output-version',
        '1',
        '--no-restore',
      ],
      acceptableExitCodes: [0],
      condition: 'nuget-findings',
      reportName: 'nuget-outdated.json',
    },
    {
      id: 'npm-production-advisories',
      cwd: npmDirectory,
      executable: 'npm',
      arguments: ['audit', '--omit=dev', '--json'],
      acceptableExitCodes: [0, 1],
      reportName: 'npm-production.json',
    },
    {
      id: 'npm-all-advisories',
      cwd: npmDirectory,
      executable: 'npm',
      arguments: ['audit', '--json'],
      acceptableExitCodes: [0, 1],
      reportName: 'npm-all.json',
    },
  ];
}

function runCommand(command) {
  return new Promise((resolvePromise) => {
    const child = spawn(command.executable, command.arguments, {
      cwd: command.cwd,
      env: process.env,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    let stdout = '';
    let stderr = '';
    let captureError = null;
    let spawnError = null;

    const append = (current, chunk) => {
      if (Buffer.byteLength(current) + chunk.length > MAX_CAPTURE_BYTES) {
        captureError = `output exceeded ${MAX_CAPTURE_BYTES} bytes`;
        child.kill('SIGTERM');
        return current;
      }
      return current + chunk.toString('utf8');
    };
    child.stdout.on('data', (chunk) => {
      stdout = append(stdout, chunk);
    });
    child.stderr.on('data', (chunk) => {
      stderr = append(stderr, chunk);
    });
    child.once('error', (error) => {
      spawnError = error.message;
    });
    child.once('close', (exitCode, signal) => {
      resolvePromise({
        exitCode,
        signal,
        stdout,
        stderr,
        error: captureError ?? spawnError,
      });
    });
  });
}

function defaultResultsDirectory(trigger) {
  const runId = `${new Date().toISOString().replaceAll(/[-:.TZ]/g, '')}-${process.pid}`;
  return resolve(REPOSITORY_ROOT, '.dependency-advisory', runId, trigger);
}

function sanitizeDiagnostic(value) {
  return String(value)
    .replaceAll(/(https?:\/\/)[^\s/@:]+:[^\s/@]+@/gi, '$1[redacted]@')
    .replaceAll(/\b(authorization|password|secret|token)\s*[=:]\s*[^\s]+/gi, '$1=[redacted]')
    .slice(0, 2_000);
}

function writeJsonAtomically(path, value) {
  const temporaryPath = `${path}.tmp-${process.pid}`;
  writeFileSync(temporaryPath, `${JSON.stringify(value, null, 2)}\n`, {
    encoding: 'utf8',
    mode: 0o600,
  });
  renameSync(temporaryPath, path);
}
