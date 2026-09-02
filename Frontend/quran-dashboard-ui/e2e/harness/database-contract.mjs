const DATABASE_MODES = new Set(['artifact', 'clone-local']);
const LOOPBACK_HOSTS = new Set(['localhost', '127.0.0.1', '::1', '[::1]']);

export const MUTABLE_TABLES = Object.freeze([
  'access_audit_events',
  'abwab_door_aliases',
  'abwab_door_inclusion_unit_syncs',
  'abwab_door_inclusions',
  'abwab_door_relations',
  'abwab_doors',
  'abwab_sections',
  'abwab_template_nodes',
  'abwab_templates',
  'linking_confirmation_jobs',
  'linking_data_state',
  'linking_door_ayah_words',
  'linking_door_ayahs',
  'linking_operations',
  'linking_prepared_affected_contributions',
  'linking_prepared_ayah_descriptions',
  'linking_prepared_ayah_words',
  'linking_prepared_ayahs',
  'linking_prepared_preflights',
  'linking_prepared_sources',
  'linking_prepared_units',
  'linking_source_contribution_units',
  'linking_source_contributions',
  'linking_unit_ayah_descriptions',
  'linking_unit_ayah_words',
  'linking_unit_ayahs',
  'linking_units',
  'linking_workspace_source_ayah_overrides',
  'linking_workspace_source_descriptions',
  'linking_workspace_source_manual_ayahs',
  'linking_workspace_source_words',
  'linking_workspace_sources',
  'linking_workspaces',
  'roles',
  'user_device_sessions',
  'user_permissions',
  'users',
]);

export function resolveDatabaseMode(environment = process.env) {
  const configured = environment.E2E_DATABASE_MODE?.trim();
  const mode = configured || 'artifact';
  if (!DATABASE_MODES.has(mode)) {
    throw new Error(
      `Unsupported E2E database mode "${mode}". Use artifact or clone-local.`,
    );
  }

  if (mode === 'clone-local' && isCiEnvironment(environment)) {
    throw new Error('CI execution accepts artifact mode only; clone-local is non-canonical.');
  }

  return mode;
}

export function parsePostgresConnectionString(connectionString) {
  const entries = parseConnectionString(connectionString);
  const host = setting(entries, ['host', 'server', 'data source'], 'Host');
  const database = setting(entries, ['database', 'initial catalog'], 'Database');
  const username = setting(entries, ['username', 'user id', 'userid', 'user'], 'Username');
  const password = setting(entries, ['password', 'pwd'], 'Password') ?? '';
  const port = setting(entries, ['port'], 'Port') ?? '5432';

  if (!host || !database || !username) {
    throw new Error('The PostgreSQL connection string must include Host, Database, and Username.');
  }
  if (!/^\d+$/.test(port)) {
    throw new Error('The PostgreSQL connection string contains an invalid Port.');
  }

  return { host, port, database, username, password };
}

export function assertLoopbackConnection(connection) {
  if (!LOOPBACK_HOSTS.has(connection.host.toLowerCase())) {
    throw new Error('E2E database access is restricted to a loopback PostgreSQL host.');
  }
}

export function assertRuntimeConnection(connection, mode) {
  if (mode === 'clone-local') {
    assertLoopbackConnection(connection);
    return;
  }
  if (mode !== 'artifact' || !isPrivateIpv4(connection.host)) {
    throw new Error(
      'Artifact database access is restricted to its private internal Docker network.',
    );
  }
}

export function withDatabase(connectionString, database) {
  const entries = parseConnectionString(connectionString);
  const matches = entries.filter(({ key }) =>
    ['database', 'initial catalog'].includes(key.toLowerCase()),
  );
  if (matches.length === 0) {
    throw new Error('The PostgreSQL connection string must include Database.');
  }
  if (matches.length > 1) {
    throw new Error('The PostgreSQL connection string contains duplicate Database settings.');
  }
  matches[0].value = database;
  return entries.map(({ key, value }) => `${key}=${encodeConnectionValue(value)}`).join(';');
}

function isCiEnvironment(environment) {
  return ['CI', 'GITHUB_ACTIONS', 'GITLAB_CI', 'TF_BUILD', 'BUILDKITE'].some((name) => {
    const value = environment[name]?.trim().toLowerCase();
    return value !== undefined && value !== '' && value !== '0' && value !== 'false';
  });
}

function isPrivateIpv4(host) {
  const octets = host.split('.').map(Number);
  if (
    octets.length !== 4
    || octets.some((octet) => !Number.isInteger(octet) || octet < 0 || octet > 255)
  ) {
    return false;
  }
  return octets[0] === 10
    || (octets[0] === 172 && octets[1] >= 16 && octets[1] <= 31)
    || (octets[0] === 192 && octets[1] === 168);
}

function parseConnectionString(connectionString) {
  const entries = [];
  let offset = 0;

  while (offset < connectionString.length) {
    while (connectionString[offset] === ';' || /\s/.test(connectionString[offset] ?? '')) {
      offset += 1;
    }
    if (offset >= connectionString.length) {
      break;
    }

    const equals = connectionString.indexOf('=', offset);
    if (equals < 0) {
      throw new Error('The PostgreSQL connection string contains an entry without a value.');
    }
    const key = connectionString.slice(offset, equals).trim();
    if (!key) {
      throw new Error('The PostgreSQL connection string contains a blank key.');
    }
    offset = equals + 1;

    while (/\s/.test(connectionString[offset] ?? '')) {
      offset += 1;
    }

    let value = '';
    const quote = connectionString[offset];
    if (quote === '"' || quote === "'") {
      offset += 1;
      let closed = false;
      while (offset < connectionString.length) {
        const character = connectionString[offset];
        if (character === quote && connectionString[offset + 1] === quote) {
          value += quote;
          offset += 2;
          continue;
        }
        if (character === quote) {
          offset += 1;
          closed = true;
          break;
        }
        value += character;
        offset += 1;
      }
      if (!closed) {
        throw new Error('The PostgreSQL connection string contains an unterminated quoted value.');
      }
      while (/\s/.test(connectionString[offset] ?? '')) {
        offset += 1;
      }
      if (offset < connectionString.length && connectionString[offset] !== ';') {
        throw new Error(
          'The PostgreSQL connection string contains trailing text after a quoted value.',
        );
      }
    } else {
      const semicolon = connectionString.indexOf(';', offset);
      const end = semicolon < 0 ? connectionString.length : semicolon;
      value = connectionString.slice(offset, end).trim();
      offset = end;
    }

    entries.push({ key, value });
    if (connectionString[offset] === ';') {
      offset += 1;
    }
  }

  return entries;
}

function setting(entries, aliases, semanticName) {
  const matches = entries.filter(({ key }) => aliases.includes(key.toLowerCase()));
  if (matches.length > 1) {
    throw new Error(
      `The PostgreSQL connection string contains duplicate ${semanticName} settings.`,
    );
  }
  return matches[0]?.value;
}

function encodeConnectionValue(value) {
  if (!value || /[;"']|^\s|\s$/.test(value)) {
    return `"${value.replaceAll('"', '""')}"`;
  }
  return value;
}
