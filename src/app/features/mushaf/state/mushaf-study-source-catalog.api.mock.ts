import { vi } from 'vitest';
import { of } from 'rxjs';

import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';

export const mushafStudySourceCatalogApiProvider = {
  provide: MushafStudySourceCatalogApi,
  useValue: {
    getCatalog: vi.fn(() =>
      of({
        isSuccess: true,
        message: 'ok',
        data: { tafsirSources: [], translationSources: [], fullI3rabSources: [] },
      }),
    ),
  },
};
