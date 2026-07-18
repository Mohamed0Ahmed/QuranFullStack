import type { Environment } from './environment.model';

/**
 * Fail-loud guard against booting a production build with placeholder Logto config.
 *
 * `environment.ts` (the production environment, wired in by `angular.json`
 * `fileReplacements`) ships literal `REPLACE-...` placeholders until the production
 * Logto tenant is provisioned. Nothing in the build pipeline catches that on its own —
 * a `production: true` bundle would silently redirect sign-in at a non-existent tenant
 * (every load firing a doomed OIDC discovery request) with no error anywhere. Calling
 * this at bootstrap (see `main.ts`) turns that into a loud runtime error instead of
 * silently shipping broken auth. Development is unaffected: `production: false` returns
 * immediately without inspecting `logto`, since the dev values are real.
 */
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
