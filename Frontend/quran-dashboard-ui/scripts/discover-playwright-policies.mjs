import { dirname, relative, resolve, sep } from 'node:path';

import {
  classifyPlaywrightPolicy,
  isNewPolicyAnnotation,
  loadPlaywrightPolicyContract,
  requireLegacyMigrationEntry,
} from './playwright-policy-contract.mjs';

export default class PlaywrightPolicyDiscoveryReporter {
  failed = false;

  onBegin(config, suite) {
    try {
      const e2eRoot = resolve(config.rootDir);
      const frontendRoot = dirname(e2eRoot);
      const contract = loadPlaywrightPolicyContract(
        resolve(e2eRoot, 'playwright-policy.json'),
        e2eRoot,
      );
      const tests = suite.allTests().map((test) => {
        const file = normalize(relative(frontendRoot, test.location.file));
        const location = `${file}:${test.location.line} (${test.title})`;
        const usesNewPolicy = (test.annotations ?? []).some(isNewPolicyAnnotation);
        const policy = usesNewPolicy
          ? classifyPlaywrightPolicy(test.annotations ?? [], contract, location)
          : requireLegacyMigrationEntry(test.location.file, e2eRoot, contract, location);

        return {
          declaredPolicy: policy.declaredPolicy,
          effectiveGroup: policy.effectiveGroup,
          file,
          fixtureProfile: policy.fixtureProfile,
          line: test.location.line,
          migrationState: usesNewPolicy ? 'Migrated' : 'Unmigrated',
          project: test.parent.project()?.name ?? 'unknown',
          title: test.title,
        };
      }).sort((left, right) =>
        `${left.file}\0${String(left.line).padStart(8, '0')}\0${left.project}\0${left.title}`
          .localeCompare(
            `${right.file}\0${String(right.line).padStart(8, '0')}\0${right.project}\0${right.title}`,
          ),
      );

      process.stdout.write(`${JSON.stringify(tests, null, 2)}\n`);
    } catch (error) {
      this.failed = true;
      process.stderr.write(`${error.message}\n`);
    }
  }

  onEnd() {
    return this.failed ? { status: 'failed' } : undefined;
  }
}

function normalize(path) {
  return path.split(sep).join('/');
}
