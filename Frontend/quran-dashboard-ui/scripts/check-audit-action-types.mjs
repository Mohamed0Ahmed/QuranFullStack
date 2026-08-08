import { readFile } from 'node:fs/promises';

const backendEnumSource = new URL(
  '../../../Backend/domain/QuranDashboard.Domain/Access/AccessAuditActionType.cs',
  import.meta.url,
);
const frontendModelSource = new URL(
  '../src/app/features/access-admin/models/access-admin.models.ts',
  import.meta.url,
);

const MINIMUM_ACTION_TYPE_COUNT = 5;
const enumBodyPattern = /public enum AccessAuditActionType\s*\{([^}]*)\}/;
const enumMemberPattern = /^(\w+)(\s*=\s*[^,]+?)?,?$/;
const declaredListPattern = /export const ACCESS_AUDIT_ACTION_TYPES = \[([^\]]*)\] as const;/;
const declaredMemberPattern = /'([^']+)'/g;

function parseBackendMembers(source) {
  const body = source.match(enumBodyPattern)?.[1];
  if (body === undefined) {
    throw new Error('AccessAuditActionType.cs no longer declares a `public enum AccessAuditActionType { … }` body.');
  }
  return body
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .map((line) => {
      const member = line.match(enumMemberPattern)?.[1];
      if (member === undefined) {
        throw new Error(
          `AccessAuditActionType.cs holds the line \`${line}\` inside the enum body, which this check cannot read as a member. ` +
            'A line it cannot read is a member it cannot compare, so it refuses to report parity over a partial reading.',
        );
      }
      return member;
    });
}

function parseFrontendMembers(source) {
  const body = source.match(declaredListPattern)?.[1];
  if (body === undefined) {
    throw new Error(
      'access-admin.models.ts no longer declares `export const ACCESS_AUDIT_ACTION_TYPES = [ … ] as const;`.',
    );
  }
  return [...body.matchAll(declaredMemberPattern)].map(([, member]) => member);
}

const [backendSource, frontendSource] = await Promise.all([
  readFile(backendEnumSource, 'utf8'),
  readFile(frontendModelSource, 'utf8'),
]);
const backendMembers = parseBackendMembers(backendSource);
const frontendMembers = parseFrontendMembers(frontendSource);

const failures = [];

if (backendMembers.length < MINIMUM_ACTION_TYPE_COUNT) {
  failures.push(
    `AccessAuditActionType.cs yielded ${backendMembers.length} members, below the floor of ${MINIMUM_ACTION_TYPE_COUNT}. ` +
      'Either this check stopped reading the enum, or the enum was emptied without lowering the floor deliberately.',
  );
}

const backendSet = new Set(backendMembers);
const frontendSet = new Set(frontendMembers);
const missingFromFrontend = backendMembers.filter((member) => !frontendSet.has(member));
const missingFromBackend = frontendMembers.filter((member) => !backendSet.has(member));

if (missingFromFrontend.length > 0) {
  failures.push(
    `ACCESS_AUDIT_ACTION_TYPES does not offer these AccessAuditActionType members: ${missingFromFrontend.join(', ')}. ` +
      'An unoffered type is missing from the audit filter dropdown and nothing else notices.',
  );
}

if (missingFromBackend.length > 0) {
  failures.push(
    `ACCESS_AUDIT_ACTION_TYPES offers values AccessAuditActionType does not declare: ${missingFromBackend.join(', ')}. ` +
      'The handler answers 400 for anything it does not recognise, so the dropdown would offer a filter that cannot run.',
  );
}

if (failures.length > 0) {
  console.error(`FAIL check-audit-action-types: ${failures.length} problem(s)`);
  for (const failure of failures) {
    console.error(`  - ${failure}`);
  }
  process.exit(1);
}

console.log(`Audit action-type parity passed (${backendMembers.length} members on both sides).`);
