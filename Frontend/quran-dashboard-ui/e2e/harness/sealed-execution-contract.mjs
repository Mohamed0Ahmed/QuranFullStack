const RECEIPT_PHASES = Object.freeze([
  'dependencyProvisioning',
  'chromiumProvisioning',
  'postgresqlProvisioning',
  'artifactProvisioning',
  'certificateProvisioning',
  'buildProvisioning',
]);

const RECEIPT_INPUTS = Object.freeze([
  'npmLockSha256',
  'nugetLocksSha256',
  'artifactLockSha256',
  'harnessSourceSha256',
  'chromiumRevision',
  'postgresqlImage',
]);

const RECEIPT_OUTPUTS = Object.freeze([
  'artifactVerifierOutput',
  'chromiumExecutable',
  'frontendBuild',
  'backendOutput',
  'tlsCertificate',
  'tlsPrivateKey',
  'egressGuard',
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
  'DOTNET_CLI_HOME',
  'DOTNET_NOLOGO',
  'DOTNET_ROOT',
  'DOTNET_SYSTEM_GLOBALIZATION_INVARIANT',
  'FORCE_COLOR',
  'HOME',
  'LANG',
  'LANGUAGE',
  'LC_ALL',
  'LOGNAME',
  'NO_COLOR',
  'NUGET_PACKAGES',
  'PATH',
  'PLAYWRIGHT_BROWSERS_PATH',
  'QDB_PR_OBSERVATION_RESULT_DIR',
  'SSL_CERT_DIR',
  'SSL_CERT_FILE',
  'TEMP',
  'TERM',
  'TMP',
  'TMPDIR',
  'TZ',
  'USER',
  'XDG_CACHE_HOME',
  'XDG_CONFIG_HOME',
  'XDG_DATA_HOME',
  'XDG_RUNTIME_DIR',
]);

export function validateProvisioningReceipt(receipt) {
  if (!receipt || receipt.schemaVersion !== 1 || receipt.status !== 'passed') {
    throw new Error('Sealed execution requires a schema-v1 passed provisioning receipt.');
  }

  requireNamedStrings(receipt.inputs, RECEIPT_INPUTS, 'input');
  requireNamedStrings(receipt.outputs, RECEIPT_OUTPUTS, 'output');
  requireNamedStrings(receipt.outputSha256, RECEIPT_OUTPUTS, 'output hash');
  for (const name of RECEIPT_OUTPUTS) {
    if (!/^[a-f0-9]{64}$/.test(receipt.outputSha256[name])) {
      throw new Error(`The provisioning receipt output hash ${name} must be SHA-256.`);
    }
  }
  if (!/^postgres@sha256:[a-f0-9]{64}$/.test(receipt.inputs.postgresqlImage)) {
    throw new Error('The provisioning receipt PostgreSQL image must be digest-pinned.');
  }

  if (!Array.isArray(receipt.phases)) {
    throw new Error('The provisioning receipt must contain phase results.');
  }
  for (const name of RECEIPT_PHASES) {
    const matches = receipt.phases.filter((phase) => phase?.name === name);
    if (
      matches.length !== 1
      || matches[0].status !== 'passed'
      || !Number.isFinite(matches[0].durationMs)
      || matches[0].durationMs < 0
    ) {
      throw new Error(`Provisioning phase ${name} must appear once and pass with a duration.`);
    }
  }
}

export function createSealedEnvironment(environment, options) {
  const sealed = createCredentialFreeEnvironment(environment);
  return {
    ...sealed,
    CI: '1',
    E2E_CHROMIUM_EXECUTABLE: options.chromiumExecutable,
    E2E_DATABASE_MODE: 'artifact',
    E2E_PREPARED_DATABASE: '1',
    E2E_SEALED_EXECUTION: '1',
    E2E_EVIDENCE_DIRECTORY: options.evidenceDirectory,
    E2E_PLAYWRIGHT_OUTPUT_DIRECTORY: options.playwrightOutputDirectory,
    E2E_TLS_CERTIFICATE: options.tlsCertificate,
    E2E_TLS_PRIVATE_KEY: options.tlsPrivateKey,
    QDB_E2E_ALLOWED_IPV4: options.databaseHost,
    LD_PRELOAD: options.egressGuard,
  };
}

export function createCredentialFreeEnvironment(environment) {
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
    if (secret.length >= 4) {
      redacted = redacted.replaceAll(secret, '[REDACTED]');
    }
  }

  return redacted
    .replace(
      /-----BEGIN ([A-Z ]*PRIVATE KEY)-----[\s\S]*?-----END \1-----/g,
      '-----BEGIN PRIVATE KEY-----[REDACTED]-----END PRIVATE KEY-----',
    )
    .replace(
      /\b(Authorization|Proxy-Authorization)\s*[:=]\s*[^\r\n]+/gi,
      '$1: [REDACTED]',
    )
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

function requireNamedStrings(value, names, semanticName) {
  if (!value || typeof value !== 'object') {
    throw new Error(`The provisioning receipt must contain ${semanticName}s.`);
  }
  for (const name of names) {
    if (typeof value[name] !== 'string' || !value[name].trim()) {
      throw new Error(`The provisioning receipt ${semanticName} ${name} is required.`);
    }
  }
}
