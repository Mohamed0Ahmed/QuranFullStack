import { writeFile } from 'node:fs/promises';

import {
  GENERATED_CODES_FILE,
  readPermissionCatalogueSource,
  renderGeneratedCodes,
} from './permission-catalogue-source.mjs';

const { catalogueCodes } = await readPermissionCatalogueSource();
await writeFile(GENERATED_CODES_FILE, renderGeneratedCodes(catalogueCodes), 'utf8');

console.log(
  `Generated ${catalogueCodes.length} permission codes into src/app/core/auth/permission-codes.generated.ts.`,
);
