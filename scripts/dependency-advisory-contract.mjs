import { existsSync, readFileSync } from 'node:fs';
import { dirname, isAbsolute, relative, resolve } from 'node:path';

const REQUIRED_TRIGGER_IDS = ['weekly', 'lockfile-change', 'release'];
const ALLOWED_REACHABILITY = new Set(['reachable', 'limited', 'not-reachable']);
const BLOCKING_SEVERITIES = new Set(['high', 'critical']);

export function loadDependencyAdvisoryContract({
  policyPath,
  repositoryRoot,
  today = new Date().toISOString().slice(0, 10),
  waiversPath,
}) {
  const policy = readJson(policyPath, 'dependency advisory policy');
  const waiversDocument = readJson(waiversPath, 'dependency advisory waivers');
  validateDependencyAdvisoryContract({ policy, repositoryRoot, today, waiversDocument });
  return { policy, waiversDocument };
}

export function validateDependencyAdvisoryContract({
  policy,
  repositoryRoot,
  today = new Date().toISOString().slice(0, 10),
  waiversDocument,
}) {
  requireDate(today, 'evaluation date');
  requireCondition(policy?.schemaVersion === 1, 'policy schemaVersion must be 1.');
  requireNonEmptyString(policy.id, 'policy id');
  requireCondition(Array.isArray(policy.triggers), 'policy triggers must be an array.');
  requireCondition(
    JSON.stringify(policy.triggers.map(({ id }) => id)) === JSON.stringify(REQUIRED_TRIGGER_IDS),
    `policy triggers must be exactly: ${REQUIRED_TRIGGER_IDS.join(', ')}.`,
  );

  const weekly = policy.triggers[0];
  const lockfileChange = policy.triggers[1];
  const release = policy.triggers[2];
  requireCondition(weekly.intervalDays === 7, 'weekly trigger intervalDays must be 7.');
  requireCondition(
    JSON.stringify(lockfileChange.paths) === JSON.stringify([
      'Backend/**/packages.lock.json',
      'Frontend/quran-dashboard-ui/package-lock.json',
    ]),
    'lockfile-change trigger must cover every NuGet lock and the npm lock.',
  );
  requireCondition(
    release.requiredBeforePromotion === true,
    'release evaluation must be required before promotion.',
  );
  for (const trigger of policy.triggers) {
    requireNonEmptyString(trigger.invocation, `${trigger.id} invocation`);
    requireCondition(
      trigger.invocation === `node scripts/run-dependency-advisory-evaluation.mjs --trigger ${trigger.id}`,
      `${trigger.id} invocation must call the provider-neutral runner with its trigger.`,
    );
  }

  requireCondition(
    JSON.stringify(policy.excludedLanes) === JSON.stringify(['nightly']),
    'nightly must be the only explicitly excluded lane.',
  );
  requireCondition(
    JSON.stringify(policy.blockingPolicy?.severities) === JSON.stringify(['high', 'critical']),
    'blocking severities must be high and critical.',
  );
  requireCondition(
    policy.blockingPolicy?.productionExposure === true,
    'blocking policy must apply to production exposure.',
  );
  requireCondition(
    policy.blockingPolicy?.confirmedReachability === 'reachable',
    'confirmed reachability must be represented as reachable.',
  );
  requireCondition(
    policy.blockingPolicy?.unassessedProductionExposure === 'block',
    'unassessed production exposure must fail closed.',
  );
  requireCondition(
    policy.blockingPolicy?.expiredWaiver === 'block',
    'expired waivers must fail closed.',
  );

  const nuget = policy.ecosystems?.nuget;
  const npm = policy.ecosystems?.npm;
  requireRepositoryFile(repositoryRoot, nuget?.solution, 'NuGet solution');
  const projectPaths = [
    ...(nuget?.projectScopes?.production ?? []),
    ...(nuget?.projectScopes?.development ?? []),
  ];
  requireCondition(projectPaths.length > 0, 'NuGet project scopes must be non-empty.');
  requireCondition(
    new Set(projectPaths).size === projectPaths.length,
    'NuGet projects must not appear in more than one scope.',
  );
  const solutionProjects = readNugetSolutionProjects(repositoryRoot, nuget.solution);
  requireCondition(
    JSON.stringify([...projectPaths].sort()) === JSON.stringify(solutionProjects),
    'NuGet project scopes must classify every solution project exactly once.',
  );
  for (const projectPath of projectPaths) {
    requireRepositoryFile(repositoryRoot, projectPath, 'NuGet project');
    requireRepositoryFile(
      repositoryRoot,
      `${dirname(projectPath)}/packages.lock.json`,
      `NuGet package lock for ${projectPath}`,
    );
  }
  requireRepositoryFile(repositoryRoot, npm?.manifest, 'npm manifest');
  requireRepositoryFile(repositoryRoot, npm?.lockfile, 'npm lockfile');
  requireNonEmptyString(npm?.directory, 'npm directory');
  requireCondition(
    resolve(repositoryRoot, npm.directory) === dirname(resolve(repositoryRoot, npm.manifest)),
    'npm directory must own the configured manifest.',
  );

  requireCondition(policy.evidence?.resultFile === 'evaluation.json', 'evaluation result file is fixed.');
  requireCondition(
    JSON.stringify(policy.evidence?.rawReports) === JSON.stringify([
      'nuget.json',
      'nuget-outdated.json',
      'npm-production.json',
      'npm-all.json',
    ]),
    'raw report names must cover NuGet advisories/candidates plus production and complete npm scans.',
  );

  requireCondition(waiversDocument?.schemaVersion === 1, 'waiver schemaVersion must be 1.');
  requireCondition(Array.isArray(waiversDocument.waivers), 'waivers must be an array.');
  const waiverIds = new Set();
  for (const waiver of waiversDocument.waivers) {
    requireNonEmptyString(waiver.id, 'waiver id');
    requireCondition(!waiverIds.has(waiver.id), `duplicate waiver id: ${waiver.id}.`);
    waiverIds.add(waiver.id);
    requireCondition(['nuget', 'npm'].includes(waiver.ecosystem), `${waiver.id} ecosystem is invalid.`);
    requireNonEmptyString(waiver.advisory, `${waiver.id} advisory`);
    requireNonEmptyString(waiver.package, `${waiver.id} package`);
    requireCondition(
      Array.isArray(waiver.dependencyPath)
        && waiver.dependencyPath.length >= 2
        && waiver.dependencyPath.every((entry) => typeof entry === 'string' && entry.length > 0),
      `${waiver.id} package dependencyPath must contain the exact reviewed path.`,
    );
    requireNonEmptyString(waiver.rationale, `${waiver.id} rationale`);
    requireNonEmptyString(waiver.owner, `${waiver.id} owner`);
    requireNonEmptyString(waiver.mitigation, `${waiver.id} mitigation`);
    requireCondition(
      ALLOWED_REACHABILITY.has(waiver.reachability),
      `${waiver.id} reachability must be reachable, limited, or not-reachable.`,
    );
    requireDate(waiver.expiresAt, `${waiver.id} expiresAt`);
    requireNonEmptyString(waiver.approvedBy, `${waiver.id} approvedBy`);
    requireDate(waiver.approvedAt, `${waiver.id} approvedAt`);
    requireCondition(
      waiver.approvedAt <= waiver.expiresAt,
      `${waiver.id} approvedAt must be on or before expiresAt.`,
    );
    requireCondition(
      waiver.approvedAt <= today,
      `${waiver.id} approvedAt must not be in the future.`,
    );
  }
}

export function loadNugetLocks(policy, repositoryRoot) {
  const projectPaths = [
    ...policy.ecosystems.nuget.projectScopes.production,
    ...policy.ecosystems.nuget.projectScopes.development,
  ];
  return Object.fromEntries(projectPaths.map((projectPath) => {
    const lockPath = resolve(repositoryRoot, dirname(projectPath), 'packages.lock.json');
    return [projectPath, readJson(lockPath, `NuGet lock for ${projectPath}`)];
  }));
}

export function evaluateDependencyAdvisories({
  evaluatedAt = new Date().toISOString(),
  nugetLocks,
  nugetOutdatedReport,
  nugetReport,
  npmAllReport,
  npmPackageLock,
  npmProductionReport,
  policy,
  repositoryRoot,
  trigger,
  waiversDocument,
}) {
  validateDependencyAdvisoryContract({
    policy,
    repositoryRoot,
    today: evaluatedAt.slice(0, 10),
    waiversDocument,
  });
  requireCondition(
    policy.triggers.some(({ id }) => id === trigger),
    `trigger must be one of: ${REQUIRED_TRIGGER_IDS.join(', ')}.`,
  );
  validateNugetReport(nugetReport);
  validateNugetReport(nugetOutdatedReport);
  validateNpmReport(npmProductionReport, 'production npm');
  validateNpmReport(npmAllReport, 'complete npm');

  const productionNpmFindings = extractNpmFindings(
    npmProductionReport,
    npmPackageLock,
    'production',
  );
  const productionNpmOccurrences = new Set(productionNpmFindings.map(npmOccurrenceIdentity));
  const developmentNpmFindings = extractNpmFindings(npmAllReport, npmPackageLock, 'all')
    .filter((finding) => !productionNpmOccurrences.has(npmOccurrenceIdentity(finding)));
  const findings = [
    ...extractNugetFindings({
      nugetLocks,
      nugetOutdatedReport,
      nugetReport,
      policy,
      repositoryRoot,
    }),
    ...productionNpmFindings,
    ...developmentNpmFindings,
  ]
    .map((finding) => applyWaiver(finding, waiversDocument.waivers, evaluatedAt))
    .sort(compareFindings);

  const expiredWaivers = waiversDocument.waivers.filter(
    (waiver) => waiver.expiresAt < evaluatedAt.slice(0, 10),
  );
  const blockingFindings = findings.filter(({ blockReason }) => blockReason !== null);
  const attachedWaiverIds = new Set(
    findings.map(({ waiver }) => waiver?.id).filter((id) => id !== undefined),
  );
  const unmatchedExpiredWaivers = expiredWaivers.filter(({ id }) => !attachedWaiverIds.has(id));
  const status = blockingFindings.length > 0 || expiredWaivers.length > 0
    ? 'blocked'
    : findings.length > 0 || waiversDocument.waivers.length > 0
      ? 'passed-with-notes'
      : 'passed';

  return {
    schemaVersion: 1,
    policyId: policy.id,
    trigger,
    evaluatedAt,
    status,
    summary: {
      total: findings.length,
      production: findings.filter(({ productionExposure }) => productionExposure).length,
      development: findings.filter(({ productionExposure }) => !productionExposure).length,
      highCriticalProduction: findings.filter(
        ({ productionExposure, severity }) => productionExposure && BLOCKING_SEVERITIES.has(severity),
      ).length,
      blocking: blockingFindings.length + unmatchedExpiredWaivers.length,
    },
    findings,
    blockingFindings,
    expiredWaivers: expiredWaivers.map(({ id, expiresAt, owner }) => ({ id, expiresAt, owner })),
  };
}

function extractNugetFindings({
  nugetLocks,
  nugetOutdatedReport,
  nugetReport,
  policy,
  repositoryRoot,
}) {
  const productionProjects = new Set(policy.ecosystems.nuget.projectScopes.production);
  const developmentProjects = new Set(policy.ecosystems.nuget.projectScopes.development);
  const findings = [];

  for (const project of nugetReport.projects ?? []) {
    const projectPath = repositoryPath(repositoryRoot, project.path);
    const productionExposure = productionProjects.has(projectPath)
      || !developmentProjects.has(projectPath);
    const scope = productionProjects.has(projectPath)
      ? 'production'
      : developmentProjects.has(projectPath)
        ? 'development'
        : 'unclassified-production';
    for (const framework of project.frameworks ?? []) {
      for (const [bucket, directness] of [
        ['topLevelPackages', 'direct'],
        ['transitivePackages', 'transitive'],
      ]) {
        for (const package_ of framework[bucket] ?? []) {
          for (const vulnerability of package_.vulnerabilities ?? []) {
            const dependencyPath = findNugetDependencyPath({
              directness,
              framework: framework.framework,
              lock: nugetLocks[projectPath],
              packageName: package_.id,
              projectPath,
            });
            const latestVersion = findNugetLatestVersion({
              framework: framework.framework,
              nugetOutdatedReport,
              packageName: package_.id,
              projectPath,
              repositoryRoot,
            });
            findings.push({
              ecosystem: 'nuget',
              package: package_.id,
              version: package_.resolvedVersion ?? null,
              severity: normalizeSeverity(vulnerability.severity),
              advisory: vulnerability.advisoryurl,
              directness,
              dependencyPath,
              pathResolution: dependencyPath === null ? 'unresolved' : 'resolved',
              productionExposure,
              scope,
              reachability: productionExposure ? 'unassessed' : 'not-applicable-development-only',
              mitigation: {
                available: latestVersion === null ? 'not-reported' : 'candidate',
                latestVersion,
                recommendation: nugetMitigationRecommendation({
                  dependencyPath,
                  directness,
                  latestVersion,
                  packageName: package_.id,
                }),
              },
              waiver: null,
              blockReason: null,
            });
          }
        }
      }
    }
  }
  return findings;
}

function extractNpmFindings(report, packageLock, rootScope) {
  const findings = [];
  const productionExposure = rootScope === 'production';
  for (const [packageName, vulnerability] of Object.entries(report.vulnerabilities ?? {})) {
    const advisoryEntries = (vulnerability.via ?? []).filter(
      (via) => via !== null && typeof via === 'object' && typeof via.url === 'string',
    );
    for (const advisory of advisoryEntries) {
      for (const targetNode of vulnerability.nodes ?? [`node_modules/${packageName}`]) {
        const dependencyPath = findNpmDependencyPath({
          packageLock,
          packageName,
          rootScope,
          targetNode,
        });
        findings.push({
          ecosystem: 'npm',
          package: packageName,
          version: packageLock.packages?.[targetNode]?.version ?? null,
          severity: normalizeSeverity(advisory.severity ?? vulnerability.severity),
          advisory: advisory.url,
          directness: dependencyPath === null
            ? 'unknown'
            : dependencyPath.length === 2
              ? 'direct'
              : 'transitive',
          dependencyPath,
          installPath: targetNode,
          pathResolution: dependencyPath === null ? 'unresolved' : 'resolved',
          productionExposure,
          scope: productionExposure ? 'production' : 'development',
          reachability: productionExposure ? 'unassessed' : 'not-applicable-development-only',
          mitigation: npmMitigation(vulnerability.fixAvailable, dependencyPath, packageName),
          waiver: null,
          blockReason: null,
        });
      }
    }
  }
  return findings;
}

function applyWaiver(finding, waivers, evaluatedAt) {
  if (!finding.productionExposure) {
    return finding;
  }
  if (finding.pathResolution === 'unresolved') {
    return { ...finding, blockReason: 'unresolved-dependency-path' };
  }

  const waiver = waivers.find((candidate) =>
    candidate.ecosystem === finding.ecosystem
    && packageNamesEqual(candidate.package, finding.package, finding.ecosystem)
    && candidate.advisory === finding.advisory
    && JSON.stringify(candidate.dependencyPath) === JSON.stringify(finding.dependencyPath));

  if (!waiver) {
    return { ...finding, blockReason: 'missing-production-waiver' };
  }

  const attached = {
    ...finding,
    reachability: waiver.reachability,
    mitigation: {
      ...finding.mitigation,
      waiverPlan: waiver.mitigation,
    },
    waiver: {
      id: waiver.id,
      rationale: waiver.rationale,
      owner: waiver.owner,
      mitigation: waiver.mitigation,
      expiresAt: waiver.expiresAt,
      approvedBy: waiver.approvedBy,
      approvedAt: waiver.approvedAt,
    },
  };
  if (waiver.expiresAt < evaluatedAt.slice(0, 10)) {
    return { ...attached, blockReason: 'expired-production-waiver' };
  }
  if (BLOCKING_SEVERITIES.has(finding.severity) && waiver.reachability === 'reachable') {
    return { ...attached, blockReason: 'confirmed-high-critical-production' };
  }
  return attached;
}

function findNugetDependencyPath({ directness, framework, lock, packageName, projectPath }) {
  if (directness === 'direct') {
    return [projectPath, packageName];
  }
  const dependencies = lock?.dependencies?.[framework];
  if (!dependencies) {
    return null;
  }
  const target = packageName.toLowerCase();
  const directPackages = Object.entries(dependencies)
    .filter(([, details]) => details.type === 'Direct')
    .map(([name]) => name);
  const queue = directPackages.map((name) => [name]);
  const visited = new Set();

  while (queue.length > 0) {
    const path = queue.shift();
    const current = path.at(-1);
    if (current.toLowerCase() === target) {
      return [projectPath, ...path];
    }
    if (visited.has(current.toLowerCase())) {
      continue;
    }
    visited.add(current.toLowerCase());
    const details = findCaseInsensitiveEntry(dependencies, current)?.[1];
    for (const child of Object.keys(details?.dependencies ?? {})) {
      queue.push([...path, child]);
    }
  }
  return null;
}

function findNpmDependencyPath({ packageLock, packageName, rootScope, targetNode }) {
  const packages = packageLock?.packages ?? {};
  const root = packages[''] ?? {};
  const roots = [...new Set([
    ...Object.keys(root.dependencies ?? {}),
    ...Object.keys(root.optionalDependencies ?? {}),
    ...(rootScope === 'all' ? Object.keys(root.devDependencies ?? {}) : []),
  ])];
  const queue = roots.map((name) => ({ name, node: resolveNpmNode('', name, packages), path: [name] }));
  const visited = new Set();

  while (queue.length > 0) {
    const current = queue.shift();
    const identity = `${current.node}:${current.name}`;
    if (visited.has(identity)) {
      continue;
    }
    visited.add(identity);
    if (current.node === targetNode || (current.name === packageName && targetNode === null)) {
      return ['Frontend/quran-dashboard-ui/package.json', ...current.path];
    }
    const details = packages[current.node] ?? {};
    const children = new Set([
      ...Object.keys(details.dependencies ?? {}),
      ...Object.keys(details.optionalDependencies ?? {}),
      ...Object.keys(details.peerDependencies ?? {}),
    ]);
    for (const child of children) {
      queue.push({
        name: child,
        node: resolveNpmNode(current.node, child, packages),
        path: [...current.path, child],
      });
    }
  }
  return null;
}

function resolveNpmNode(parentNode, dependencyName, packages) {
  let search = parentNode;
  while (true) {
    const candidate = search.length > 0
      ? `${search}/node_modules/${dependencyName}`
      : `node_modules/${dependencyName}`;
    if (Object.hasOwn(packages, candidate)) {
      return candidate;
    }
    const marker = search.lastIndexOf('/node_modules/');
    if (marker < 0) {
      return `node_modules/${dependencyName}`;
    }
    search = search.slice(0, marker);
  }
}

function npmMitigation(fixAvailable, dependencyPath, packageName) {
  const available = fixAvailable === true || (fixAvailable !== null && typeof fixAvailable === 'object');
  const parent = dependencyPath?.length > 2 ? dependencyPath[1] : packageName;
  const major = typeof fixAvailable === 'object' && fixAvailable.isSemVerMajor === true;
  return {
    available,
    changeType: major ? 'major-breaking' : available ? 'non-major-or-unspecified' : 'no-fix-reported',
    recommendation: major
      ? `Optional breaking upgrade: evaluate ${parent} at ${fixAvailable.version ?? 'the reported major version'} before changing the committed npm lock.`
      : available
        ? `Upgrade ${parent}${parent === packageName ? '' : `, the parent of ${packageName}`}, through the committed npm lock.`
      : `No npm fix is currently reported for ${packageName}; document compensating controls and re-evaluate before waiver expiry.`,
    scannerFix: typeof fixAvailable === 'object' ? fixAvailable : fixAvailable === true,
  };
}

function nugetMitigationRecommendation({ dependencyPath, directness, latestVersion, packageName }) {
  if (dependencyPath === null) {
    return `Resolve the exact parent path before assessing an upgrade for ${packageName}.`;
  }
  const target = directness === 'direct' ? packageName : dependencyPath.at(-2);
  if (latestVersion !== null) {
    return directness === 'direct'
      ? `Evaluate direct package ${packageName} at NuGet candidate ${latestVersion}.`
      : `NuGet reports ${packageName} candidate ${latestVersion}; upgrade parent ${target} to a version that resolves a fixed transitive package instead of adding an unexplained direct pin.`;
  }
  return `NuGet reports no newer candidate for ${packageName}; document a compensating control and re-evaluate before waiver expiry.`;
}

function findNugetLatestVersion({
  framework,
  nugetOutdatedReport,
  packageName,
  projectPath,
  repositoryRoot,
}) {
  const project = (nugetOutdatedReport.projects ?? []).find(
    (candidate) => repositoryPath(repositoryRoot, candidate.path) === projectPath,
  );
  const targetFramework = (project?.frameworks ?? []).find(
    (candidate) => candidate.framework === framework,
  );
  const package_ = [
    ...(targetFramework?.topLevelPackages ?? []),
    ...(targetFramework?.transitivePackages ?? []),
  ].find((candidate) => candidate.id.toLowerCase() === packageName.toLowerCase());
  return package_?.latestVersion ?? null;
}

function validateNugetReport(report) {
  requireCondition(report?.version === 1, 'NuGet report version must be 1.');
  requireCondition(!report.problems?.some(({ level }) => level === 'error'), 'NuGet scan reported an error.');
  requireCondition(Array.isArray(report.projects), 'NuGet report projects must be an array.');
}

export function nugetReportHasFindings(report) {
  return (report?.projects ?? []).some((project) =>
    (project.frameworks ?? []).some((framework) =>
      ['topLevelPackages', 'transitivePackages'].some((bucket) =>
        (framework[bucket] ?? []).some((package_) =>
          (package_.vulnerabilities ?? []).length > 0))));
}

function validateNpmReport(report, name) {
  requireCondition(report?.auditReportVersion === 2, `${name} report version must be 2.`);
  requireCondition(!report.error, `${name} scan reported an error.`);
  requireCondition(
    report.vulnerabilities !== null && typeof report.vulnerabilities === 'object',
    `${name} vulnerabilities must be an object.`,
  );
}

function findingIdentity(finding) {
  return [
    finding.ecosystem,
    finding.package.toLowerCase(),
    finding.advisory,
    JSON.stringify(finding.dependencyPath),
  ].join('|');
}

function npmOccurrenceIdentity(finding) {
  return [finding.package, finding.advisory, finding.installPath].join('|');
}

function compareFindings(left, right) {
  return findingIdentity(left).localeCompare(findingIdentity(right));
}

function packageNamesEqual(left, right, ecosystem) {
  return ecosystem === 'nuget' ? left.toLowerCase() === right.toLowerCase() : left === right;
}

function findCaseInsensitiveEntry(object, key) {
  return Object.entries(object).find(([candidate]) => candidate.toLowerCase() === key.toLowerCase());
}

function repositoryPath(repositoryRoot, path) {
  const normalized = isAbsolute(path) ? relative(repositoryRoot, path) : path;
  return normalized.split('\\').join('/');
}

function normalizeSeverity(severity) {
  const normalized = String(severity).toLowerCase();
  requireCondition(
    ['info', 'low', 'moderate', 'high', 'critical'].includes(normalized),
    `unsupported advisory severity: ${severity}.`,
  );
  return normalized;
}

function readJson(path, name) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read ${name} ${path}: ${error.message}`);
  }
}

function readNugetSolutionProjects(repositoryRoot, solutionPath) {
  const absoluteSolutionPath = resolve(repositoryRoot, solutionPath);
  const solutionDirectory = dirname(absoluteSolutionPath);
  const solution = readFileSync(absoluteSolutionPath, 'utf8');
  return [...solution.matchAll(/"([^"]+\.csproj)"/g)]
    .map((match) => repositoryPath(
      repositoryRoot,
      resolve(solutionDirectory, match[1].replaceAll('\\', '/')),
    ))
    .sort();
}

function requireRepositoryFile(repositoryRoot, path, name) {
  requireNonEmptyString(path, `${name} path`);
  const absolutePath = resolve(repositoryRoot, path);
  const relativePath = relative(repositoryRoot, absolutePath);
  requireCondition(
    relativePath === '' || (!relativePath.startsWith('..') && !isAbsolute(relativePath)),
    `${name} must stay inside the repository.`,
  );
  requireCondition(existsSync(absolutePath), `${name} is missing: ${path}.`);
}

function requireDate(value, name) {
  requireCondition(/^\d{4}-\d{2}-\d{2}$/.test(value), `${name} must use YYYY-MM-DD.`);
  const parsed = new Date(`${value}T00:00:00.000Z`);
  requireCondition(
    !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value,
    `${name} must be a real calendar date.`,
  );
}

function requireCondition(condition, message) {
  if (!condition) {
    throw new Error(`Invalid dependency advisory contract: ${message}`);
  }
}

function requireNonEmptyString(value, name) {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string.`);
}
