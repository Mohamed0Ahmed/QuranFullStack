const { cleanupOwnedDatabaseRuntime } = await import(process.argv[2]);
const cleanup = cleanupOwnedDatabaseRuntime();
process.exit(cleanup.status === 'passed' ? 0 : 1);
