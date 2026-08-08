import { readFile } from 'node:fs/promises';

import {
  GENERATED_CODES_FILE,
  readPermissionCatalogueSource,
  renderGeneratedCodes,
} from './permission-catalogue-source.mjs';

const MINIMUM_PERMISSION_COUNT = 10;

const failures = [];
const { declaredCodes, catalogueCodes, catalogueEntryCount } = await readPermissionCatalogueSource();
const generatedFile = await readFile(GENERATED_CODES_FILE, 'utf8');

if (declaredCodes.length < MINIMUM_PERMISSION_COUNT) {
  failures.push(
    `AbwabPermissions.cs yielded ${declaredCodes.length} permission constants, below the floor of ${MINIMUM_PERMISSION_COUNT}. ` +
      'Either this check stopped reading the source, or permissions were removed wholesale without lowering the floor deliberately.',
  );
}

if (catalogueEntryCount !== catalogueCodes.length) {
  failures.push(
    `AbwabPermissionCatalogue.cs holds ${catalogueEntryCount} definitions but only ${catalogueCodes.length} name an AbwabPermissions constant. ` +
      'A definition that spells its code as a literal is invisible to every consumer of the constants.',
  );
}

const catalogued = new Set(catalogueCodes);
const unusedConstants = declaredCodes.filter((code) => !catalogued.has(code));
if (unusedConstants.length > 0) {
  failures.push(
    `AbwabPermissions.cs declares codes that AbwabPermissionCatalogue.cs never defines: ${unusedConstants.join(', ')}. ` +
      'An undefined code is never served, never synchronized into the permissions table, and never assignable.',
  );
}

const duplicateCodes = [
  ...new Set(catalogueCodes.filter((code, index) => catalogueCodes.indexOf(code) !== index)),
];
if (duplicateCodes.length > 0) {
  failures.push(
    `AbwabPermissionCatalogue.cs defines these permission codes more than once: ${duplicateCodes.join(', ')}. ` +
      'A duplicated definition makes the generated allowlist disagree with the catalogue it is generated from.',
  );
}

if (generatedFile !== renderGeneratedCodes(catalogueCodes)) {
  failures.push(
    'src/app/core/auth/permission-codes.generated.ts disagrees with AbwabPermissionCatalogue.cs. ' +
      'Run `npm run generate:permission-codes` and commit the result, so adding or retiring a permission is one visible change.',
  );
}

if (failures.length > 0) {
  console.error(`FAIL check-permission-catalogue: ${failures.length} problem(s)`);
  for (const failure of failures) {
    console.error(`  - ${failure}`);
  }
  process.exit(1);
}

console.log(
  `Permission catalogue parity passed (${catalogueCodes.length} codes across the constants, the catalogue definitions and the generated frontend allowlist).`,
);
