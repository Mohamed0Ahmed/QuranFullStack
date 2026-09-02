import { appendFileSync } from 'node:fs';
import { resolve } from 'node:path';

const log = resolve(process.env.QDB_NIGHTLY_RESULTS_DIR, 'lifecycle-order.log');
appendFileSync(log, 'timeout:start\n');
process.on('SIGTERM', () => appendFileSync(log, 'timeout:term\n'));
setInterval(() => {}, 1_000);
