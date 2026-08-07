import { existsSync, readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const projectName = 'quran-dashboard-ui';

const PRIMARY_AREA_CONFIGURATIONS = [
  'feature-access-admin',
  'feature-abwab',
  'feature-auth',
  'feature-dashboard',
  'feature-mushaf',
  'feature-words',
  'shared',
];

const REQUIRED_CONFIGURATIONS = [
  'feature-access-admin',
  'feature-abwab',
  'feature-auth',
  'feature-dashboard',
  'feature-mushaf',
  'feature-words',
  'authorization',
  'composition',
  'shared',
  'fast',
];

const COMPOSITION_MEMBER_PATTERNS = [
  'src/app/**/*.component.spec.ts',
  'src/app/**/*.directive.spec.ts',
  'src/app/app.nested-layers.spec.ts',
  'src/app/core/navigation/detail-overlay/detail-overlay-history.bootstrap.spec.ts',
  'src/app/core/navigation/detail-overlay/detail-overlay-history.service.spec.ts',
  'src/app/features/words/entity-detail-overlay/entity-detail-overlay-ayah-continuity.spec.ts',
  'src/app/features/words/entity-detail-overlay/entity-detail-overlay-invariant.spec.ts',
];

const AUTHORIZATION_BOUNDARY_PATTERNS = [
  'src/app/app.config.auth.spec.ts',
  'src/app/app.routes.spec.ts',
  'src/app/core/auth/**/*.spec.ts',
  'src/app/core/data-access/secure-url.interceptor.spec.ts',
  'src/app/features/auth/**/*.spec.ts',
  'src/environments/environment-guard.spec.ts',
];

function escapeLiteral(text) {
  return text.replace(/[.+^${}()|[\]\\]/g, '\\$&');
}

function segmentToSource(segment) {
  return escapeLiteral(segment).replace(/\*/g, '[^/]*').replace(/\?/g, '[^/]');
}

function globToRegExp(pattern) {
  const segments = pattern.split('/');
  let source = '^';
  segments.forEach((segment, index) => {
    const isLast = index === segments.length - 1;
    if (segment === '**') {
      source += isLast ? '.*' : '(?:[^/]*/)*';
      return;
    }
    source += segmentToSource(segment);
    if (!isLast) {
      source += '/';
    }
  });
  return new RegExp(`${source}$`);
}

function matchingFiles(pattern, files) {
  const expression = globToRegExp(pattern);
  return files.filter((file) => expression.test(file));
}

function selectionOf(includePatterns, files) {
  const selected = new Set();
  for (const pattern of includePatterns) {
    for (const file of matchingFiles(pattern, files)) {
      selected.add(file);
    }
  }
  return selected;
}

function inventory(relativeDirectory, suffix) {
  const absolute = path.join(projectRoot, relativeDirectory);
  if (!existsSync(absolute)) {
    return [];
  }
  return readdirSync(absolute, { recursive: true })
    .map((entry) => `${relativeDirectory}/${entry.split(path.sep).join('/')}`)
    .filter((entry) => entry.endsWith(suffix))
    .sort();
}

function report(lines) {
  for (const line of lines) {
    console.log(line);
  }
}

function fail(failures) {
  console.log('');
  console.log(`FAIL verify-test-gates: ${failures.length} problem(s)`);
  for (const failure of failures) {
    console.log(`  - ${failure}`);
  }
  process.exit(1);
}

const angularJsonPath = path.join(projectRoot, 'angular.json');
const angularJson = JSON.parse(readFileSync(angularJsonPath, 'utf8'));
const testTarget = angularJson.projects?.[projectName]?.architect?.test;

if (!testTarget) {
  fail([`angular.json has no projects.${projectName}.architect.test target`]);
}

const baseInclude = testTarget.options?.include ?? [];
const configurations = testTarget.configurations ?? {};
const specFiles = inventory('src', '.spec.ts');
const e2eFiles = inventory('e2e', '.e2e.ts');

report([
  `angular.json: ${angularJsonPath}`,
  `spec files under src/: ${specFiles.length}`,
  `e2e files under e2e/: ${e2eFiles.length}`,
  `full gate include: ${JSON.stringify(baseInclude)}`,
  '',
]);

if (specFiles.length === 0) {
  fail(['no src/**/*.spec.ts files were found; the inventory is empty']);
}

const missingConfigurations = REQUIRED_CONFIGURATIONS.filter((name) => !configurations[name]);
if (missingConfigurations.length > 0) {
  fail(
    missingConfigurations.map(
      (name) => `missing configuration: angular.json architect.test.configurations.${name}`,
    ),
  );
}

const configurationsWithoutInclude = REQUIRED_CONFIGURATIONS.filter(
  (name) => !Array.isArray(configurations[name].include) || configurations[name].include.length === 0,
);
if (configurationsWithoutInclude.length > 0) {
  fail(
    configurationsWithoutInclude.map(
      (name) => `configuration ${name} has no non-empty include array`,
    ),
  );
}

const selections = new Map(
  REQUIRED_CONFIGURATIONS.map((name) => [name, selectionOf(configurations[name].include, specFiles)]),
);

report(REQUIRED_CONFIGURATIONS.map((name) => `${name}: ${selections.get(name).size} spec file(s)`));

const failures = [];

const fullGate = selectionOf(baseInclude, specFiles);
for (const file of specFiles) {
  if (!fullGate.has(file)) {
    failures.push(`full gate does not select ${file}`);
  }
}

for (const file of specFiles) {
  const areas = PRIMARY_AREA_CONFIGURATIONS.filter((name) => selections.get(name).has(file));
  if (areas.length !== 1) {
    failures.push(
      `${file} has ${areas.length} primary areas (${areas.join(', ') || 'none'}); exactly one is required`,
    );
  }
}

for (const pattern of COMPOSITION_MEMBER_PATTERNS) {
  const matched = matchingFiles(pattern, specFiles);
  if (matched.length === 0) {
    failures.push(`composition membership pattern matches no file: ${pattern}`);
    continue;
  }
  for (const file of matched) {
    if (!selections.get('composition').has(file)) {
      failures.push(`composition does not select ${file} (required by ${pattern})`);
    }
  }
}

for (const pattern of AUTHORIZATION_BOUNDARY_PATTERNS) {
  const matched = matchingFiles(pattern, specFiles);
  if (matched.length === 0) {
    failures.push(`authorization boundary pattern matches no file: ${pattern}`);
    continue;
  }
  for (const file of matched) {
    if (!selections.get('authorization').has(file)) {
      failures.push(`authorization does not select ${file} (required by ${pattern})`);
    }
  }
}

const allIncludeSets = [['<full gate>', baseInclude]].concat(
  REQUIRED_CONFIGURATIONS.map((name) => [name, configurations[name].include]),
);

for (const [name, includePatterns] of allIncludeSets) {
  for (const pattern of includePatterns) {
    if (matchingFiles(pattern, specFiles).length === 0) {
      failures.push(`${name} include pattern is a dead path (matches no spec file): ${pattern}`);
    }
    const leakedE2e = matchingFiles(pattern, e2eFiles);
    if (leakedE2e.length > 0) {
      failures.push(`${name} include pattern selects e2e files (${leakedE2e.join(', ')}): ${pattern}`);
    }
  }
}

if (failures.length > 0) {
  fail(failures);
}

console.log('');
console.log(`PASS verify-test-gates: ${specFiles.length} spec file(s), ${REQUIRED_CONFIGURATIONS.length} configurations`);
