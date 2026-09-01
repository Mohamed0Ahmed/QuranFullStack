import { appendFileSync } from 'node:fs';
import { resolve } from 'node:path';

appendFileSync(resolve(process.argv[2], 'lifecycle-order.log'), `${process.argv[3]}:cleanup\n`);
process.exit(process.argv[3] === 'diagnostic' ? 1 : 0);
