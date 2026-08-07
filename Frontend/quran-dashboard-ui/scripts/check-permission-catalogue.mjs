import { readFile } from 'node:fs/promises';

const backendPermissionSource = new URL(
  '../../../Backend/application/QuranDashboard.Application.Abstractions/Security/Permissions/AbwabPermissions.cs',
  import.meta.url,
);
const frontendPermissionSource = new URL('../src/app/core/auth/permission-code.ts', import.meta.url);
const permissionPattern = /['"](abwab\.[^'"]+)['"]/g;

function readPermissionCodes(source) {
  return [...source.matchAll(permissionPattern)].map((match) => match[1]).sort();
}

const [backendSource, frontendSource] = await Promise.all([
  readFile(backendPermissionSource, 'utf8'),
  readFile(frontendPermissionSource, 'utf8'),
]);
const backendCodes = readPermissionCodes(backendSource);
const frontendCodes = readPermissionCodes(frontendSource);

if (JSON.stringify(backendCodes) !== JSON.stringify(frontendCodes)) {
  throw new Error(
    `Permission catalogue mismatch. Backend: ${backendCodes.join(', ')}. Frontend: ${frontendCodes.join(', ')}.`,
  );
}

console.log(`Permission catalogue parity passed (${backendCodes.length} codes).`);
