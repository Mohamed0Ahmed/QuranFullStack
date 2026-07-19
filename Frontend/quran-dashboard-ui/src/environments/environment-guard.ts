import type { Environment } from './environment.model';

// The production `environment.ts` ships literal `REPLACE-...` Logto placeholders
// until the tenant is provisioned, and nothing in the build catches that — a
// `production: true` bundle would silently redirect sign-in to a non-existent tenant
// (every load firing a doomed OIDC discovery) with no error anywhere. Called at
// bootstrap (main.ts) to turn that into a loud runtime error. Dev is unaffected:
// `production: false` returns before inspecting `logto`, whose dev values are real.
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
}
