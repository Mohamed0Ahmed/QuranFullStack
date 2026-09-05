import { resolve } from 'node:path';

const RECEIPT_PHASES = Object.freeze([
  'dependencyProvisioning',
  'chromiumProvisioning',
  'certificateProvisioning',
  'buildProvisioning',
]);

const RECEIPT_INPUTS = Object.freeze([
  'chromiumRevision',
  'chromiumVersion',
  'harnessSourceSha256',
  'npmLockSha256',
  'nugetLocksSha256',
]);

const RECEIPT_OUTPUTS = Object.freeze([
  'backendOutput',
  'chromiumExecutable',
  'egressGuard',
  'frontendBuild',
  'testRuntimeOutput',
  'tlsCertificate',
  'tlsPrivateKey',
]);

const SENSITIVE_ENVIRONMENT_NAMES = [
  /^(?:ARTIFACT|AWS|AZURE_STORAGE|GOOGLE_APPLICATION_CREDENTIALS)(?:_|$)/i,
  /^(?:NPM|NODE_AUTH|NUGET_AUTH|PLAYWRIGHT_DOWNLOAD_CONNECTION)_TOKEN$/i,
  /^(?:ConnectionStrings__|PGPASSWORD$)/i,
  /(?:^|_)(?:CONNECTION_STRING|CREDENTIALS?|PASSWORD|PASSWD|PRIVATE_KEY|SECRET|SIGNED_URL|TOKEN)$/i,
  /(?:DOCKER_AUTH_CONFIG|SYSTEM_ACCESSTOKEN|VSS_NUGET_EXTERNAL_FEED_ENDPOINTS)/i,
];

const SAFE_ENVIRONMENT_NAMES = new Set([
  'COLORTERM',
  'DBUS_SESSION_BUS_ADDRESS',
  'DISPLAY',
  'DOTNET_CLI_HOME',
  'DOTNET_NOLOGO',
  'DOTNET_ROOT',
  'DOTNET_SYSTEM_GLOBALIZATION_INVARIANT',
  'FORCE_COLOR',
  'LANG',
  'LANGUAGE',
  'LC_ALL',
  'LOGNAME',
  'NO_COLOR',
  'NUGET_PACKAGES',
  'PATH',
  'PLAYWRIGHT_BROWSERS_PATH',
  'QDB_PR_OBSERVATION_RESULT_DIR',
  'QURAN_DASHBOARD_RUN_EVIDENCE_PATH',
  'QURAN_DASHBOARD_TEST_COMMAND_ID',
  'SSL_CERT_DIR',
  'SSL_CERT_FILE',
  'TEMP',
  'TERM',
  'TMP',
  'TMPDIR',
  'TZ',
  'USER',
  'WAYLAND_DISPLAY',
  'XAUTHORITY',
  'XDG_RUNTIME_DIR',
  'XDG_SESSION_TYPE',
]);

export function validateControlledProvisioningReceipt(receipt) {
  if (!receipt || receipt.schemaVersion !== 2 || receipt.status !== 'passed') {
    throw new Error('Controlled execution requires a schema-v2 passed provisioning receipt.');
  }

  requireExactNamedStrings(receipt.inputs, RECEIPT_INPUTS, 'input');
  requireExactNamedStrings(receipt.outputs, RECEIPT_OUTPUTS, 'output');
  requireExactNamedStrings(receipt.outputSha256, RECEIPT_OUTPUTS, 'output hash');
  for (const name of RECEIPT_OUTPUTS) {
    if (!/^[a-f0-9]{64}$/.test(receipt.outputSha256[name])) {
      throw new Error(`The controlled provisioning output hash ${name} must be SHA-256.`);
    }
  }

  if (!Array.isArray(receipt.phases)) {
    throw new Error('Controlled provisioning must contain phase results.');
  }
  for (const name of RECEIPT_PHASES) {
    const matches = receipt.phases.filter((phase) => phase?.name === name);
    if (
      matches.length !== 1
      || matches[0].status !== 'passed'
      || !Number.isFinite(matches[0].durationMs)
      || matches[0].durationMs < 0
    ) {
      throw new Error(`Controlled provisioning phase ${name} must appear once and pass with a duration.`);
    }
  }
  const expectedPhases = new Set(RECEIPT_PHASES);
  const unexpectedPhase = receipt.phases.find((phase) => !expectedPhases.has(phase?.name));
  if (unexpectedPhase || receipt.phases.length !== RECEIPT_PHASES.length) {
    throw new Error(
      `Controlled provisioning contains an unexpected phase ${unexpectedPhase?.name ?? 'entry'}.`,
    );
  }
}

export function createControlledEnvironment(environment, options) {
  const connectionString = environment.ConnectionStrings__QuranDashboardTest?.trim();
  if (!connectionString) {
    throw new Error(
      'Controlled Playwright execution requires ConnectionStrings__QuranDashboardTest.',
    );
  }
  for (const [name, value] of Object.entries(options)) {
    if (typeof value !== 'string' || !value.startsWith('/')) {
      throw new Error(`Controlled Playwright execution requires an absolute ${name} path.`);
    }
  }

  const controlled = createCredentialStrippedEnvironment(environment);
  return {
    ...controlled,
    CI: '1',
    ConnectionStrings__QuranDashboardTest: connectionString,
    E2E_BACKEND_ASSEMBLY: options.backendAssembly,
    E2E_CHROMIUM_EXECUTABLE: options.chromiumExecutable,
    E2E_CONTROLLED_EXECUTION: '1',
    E2E_EVIDENCE_DIRECTORY: options.evidenceDirectory,
    E2E_FRONTEND_BUILD: options.frontendBuild,
    E2E_PLAYWRIGHT_OUTPUT_DIRECTORY: options.playwrightOutputDirectory,
    E2E_TEST_RUNTIME_ASSEMBLY: options.testRuntimeAssembly,
    E2E_TLS_CERTIFICATE: options.tlsCertificate,
    E2E_TLS_PRIVATE_KEY: options.tlsPrivateKey,
    HOME: options.homeDirectory,
    LD_PRELOAD: options.egressGuard,
    XDG_CACHE_HOME: resolve(options.homeDirectory, '.cache'),
    XDG_CONFIG_HOME: resolve(options.homeDirectory, '.config'),
    XDG_DATA_HOME: resolve(options.homeDirectory, '.local/share'),
  };
}

export function buildControlledBackendArguments(backendAssembly, apiProject, controlled) {
  const arguments_ = backendAssembly
    ? [backendAssembly]
    : ['run', '--project', apiProject, '--no-build', '--no-restore', '--no-launch-profile'];
  if (controlled) {
    if (!backendAssembly) arguments_.push('--');
    arguments_.push('--Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Warning');
  }
  return arguments_;
}

export function createCredentialStrippedEnvironment(environment) {
  const safe = {};
  for (const [name, value] of Object.entries(environment)) {
    if (SAFE_ENVIRONMENT_NAMES.has(name) || name.startsWith('LC_')) {
      safe[name] = value;
    }
  }
  return safe;
}

export function sensitiveEnvironmentValues(environment) {
  return Object.entries(environment)
    .filter(([name, value]) => isSensitiveEnvironmentName(name) && value?.length >= 4)
    .map(([, value]) => value);
}

export function redactDiagnosticText(value, knownSecrets = []) {
  let redacted = String(value);
  for (const secret of [...new Set(knownSecrets)].sort((left, right) => right.length - left.length)) {
    if (secret.length >= 4) redacted = redacted.replaceAll(secret, '[REDACTED]');
  }

  return redacted
    .replace(
      /-----BEGIN ([A-Z ]*PRIVATE KEY)-----[\s\S]*?-----END \1-----/g,
      '-----BEGIN PRIVATE KEY-----[REDACTED]-----END PRIVATE KEY-----',
    )
    .replace(/\b(Authorization|Proxy-Authorization)\s*[:=]\s*[^\r\n]+/gi, '$1: [REDACTED]')
    .replace(/\b(?:Cookie|Set-Cookie)\s*[:=]\s*[^\r\n]+/gi, (line) => {
      const separator = line.indexOf(':') >= 0 ? ':' : '=';
      return `${line.slice(0, line.indexOf(separator))}${separator} [REDACTED]`;
    })
    .replace(/\b(Password|Pwd)\s*=\s*(?:"[^"]*"|'[^']*'|[^;\s\r\n]*)/gi, '$1=[REDACTED]')
    .replace(
      /(["'](?:access[_-]?token|authorization|client[_-]?secret|cookie|password|refresh[_-]?token|signed[_-]?url)["']\s*:\s*)["'][^"']*["']/gi,
      '$1"[REDACTED]"',
    )
    .replace(/\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b/g, '[REDACTED]')
    .replace(/(https?:\/\/[^\s?#]+)\?[^\s#]*/gi, '$1?[REDACTED]');
}

export function isSensitiveEnvironmentName(name) {
  return !SAFE_ENVIRONMENT_NAMES.has(name)
    && !name.startsWith('LC_')
    && SENSITIVE_ENVIRONMENT_NAMES.some((pattern) => pattern.test(name));
}

function requireExactNamedStrings(value, names, semanticName) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error(`Controlled provisioning must contain ${semanticName}s.`);
  }
  const expected = new Set(names);
  for (const name of Object.keys(value)) {
    if (!expected.has(name)) {
      throw new Error(`Controlled provisioning contains unexpected ${semanticName} ${name}.`);
    }
  }
  for (const name of names) {
    if (typeof value[name] !== 'string' || !value[name].trim()) {
      throw new Error(`Controlled provisioning ${semanticName} ${name} is required.`);
    }
  }
}
