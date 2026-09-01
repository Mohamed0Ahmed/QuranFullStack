import assert from 'node:assert/strict';
import { chmodSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = mkdtempSync(resolve(tmpdir(), 'qdb-cleanup-proof-'));
const script = resolve(dirname(fileURLToPath(import.meta.url)), '../Backend/scripts/cleanup-test-runtime');
const runId = '0123456789abcdef0123456789abcdef';
try {
  verify('zero', 0);
  verify('ps-error', 1);
  verify('network-error', 1);
  verify('resources', 1);
  verify('dry-proof', 2);
} finally {
  rmSync(root, { force: true, recursive: true });
}
console.log('Release candidate fake-Docker cleanup verifier passed.');

function verify(mode, expectedStatus) {
  const bin = resolve(root, mode);
  const docker = resolve(bin, 'docker');
  mkdirSync(bin, { recursive: true });
  writeFileSync(docker, `#!/usr/bin/env bash\ncase "$1 $2" in\n  "info ") exit 0 ;;\n  "ps -aq") [[ "${mode}" == ps-error ]] && exit 9; [[ "${mode}" == resources ]] && printf 'container\\n'; exit 0 ;;\n  "network ls") [[ "${mode}" == network-error ]] && exit 9; [[ "${mode}" == resources ]] && printf 'network\\n'; exit 0 ;;\n  "ps -q") exit 0 ;;\n  "rm --force"|"network rm") exit 0 ;;\n  *) exit 0 ;;\nesac\n`);
  chmodSync(docker, 0o755);
  let status = 0;
  const arguments_ = ['--run-id', runId, '--require-proof'];
  if (mode === 'dry-proof') arguments_.push('--dry-run');
  try { execFileSync('bash', [script, ...arguments_], { env: { ...process.env, PATH: `${bin}:${process.env.PATH}` }, stdio: 'pipe' }); } catch (error) { status = error.status ?? 1; }
  assert.equal(status, expectedStatus, mode);
}
