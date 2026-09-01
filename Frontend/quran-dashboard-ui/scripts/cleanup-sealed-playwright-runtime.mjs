import { cleanupOwnedDatabaseRuntime } from '../e2e/harness/database-runtime.mjs';

const cleanup = cleanupOwnedDatabaseRuntime();
process.exit(cleanup.status === 'passed' ? 0 : 1);
