import type { Environment } from './environment.model';

export function assertProductionAuthConfigured(env: Environment): void {
  if (!env.production) {
    return;
  }

  const offendingKeys = Object.entries(env.logto)
    .filter(([, value]) => typeof value === 'string' && value.toUpperCase().includes('REPLACE'))
    .map(([key]) => key);

  if (offendingKeys.length > 0) {
    throw new Error(
      `Production build is shipping placeholder Logto config for: ${offendingKeys.join(', ')}. ` +
        'Set real values in src/environments/environment.ts before deploying.',
    );
  }

  if (!env.logto.scope.split(/\s+/).includes('email')) {
    throw new Error('Production Logto config must request the email scope.');
  }
}
