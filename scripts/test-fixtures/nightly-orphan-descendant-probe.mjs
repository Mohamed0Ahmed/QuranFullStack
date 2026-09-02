import { spawn } from 'node:child_process';
import { appendFileSync } from 'node:fs';
import { resolve } from 'node:path';

const log = resolve(process.env.QDB_NIGHTLY_RESULTS_DIR, 'lifecycle-order.log');
appendFileSync(log, 'orphan:parent-start\n');
spawn(process.execPath, ['-e', `
  const { appendFileSync } = require('node:fs');
  const log = process.argv[1];
  process.on('SIGTERM', () => {});
  setInterval(() => appendFileSync(log, 'orphan:descendant-alive\\n'), 50);
`, log], { detached: false, stdio: 'ignore' });
process.on('SIGTERM', () => {
  appendFileSync(log, 'orphan:parent-term\n');
  process.exit(0);
});
setInterval(() => {}, 1_000);
