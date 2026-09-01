import { appendFileSync } from 'node:fs';
import { resolve } from 'node:path';

appendFileSync(resolve(process.env.QDB_NIGHTLY_RESULTS_DIR, 'lifecycle-order.log'), 'database-work\n');
