import { Injectable, inject, signal } from '@angular/core';

import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { SourceOption } from '../models/mushaf.models';
import {
  fullI3rabCatalogItemToOption,
  tafsirCatalogItemToOption,
  translationCatalogItemToOption,
} from '../utils/study-source-catalog.labels';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';

/**
 * Owns the reader's study-source catalogue (tafsir / translation / full-إعراب options).
 * The catalogue is reader-wide reference data: it is not part of the reader's URL state
 * machine and does not depend on the page / ayah / word selection, so it loads and caches
 * itself here rather than inside {@link MushafReaderFacade}.
 *
 * F2: {@link load} guards the fetch so it fires GET /api/mushaf/study-sources at most once
 * per successful load. `loaded` flips only on a genuine success (even an empty-but-successful
 * catalogue counts) so it is distinguishable from "not loaded yet"; a failure never sets
 * `loaded`, so the next mount can retry.
 */
@Injectable({ providedIn: 'root' })
export class MushafStudySourceCatalogStore {
  private readonly catalogApi = inject(MushafStudySourceCatalogApi);

  private readonly _tafsirSourceOptions = signal<SourceOption[]>([]);
  private readonly _translationSourceOptions = signal<SourceOption[]>([]);
  private readonly _fullI3rabSourceOptions = signal<SourceOption[]>([]);

  private loaded = false;
  private loading = false;

  readonly tafsirSourceOptions = this._tafsirSourceOptions.asReadonly();
  readonly translationSourceOptions = this._translationSourceOptions.asReadonly();
  readonly fullI3rabSourceOptions = this._fullI3rabSourceOptions.asReadonly();

  load(): void {
    if (this.loaded || this.loading) {
      return;
    }

    this.loading = true;
    subscribeToApiLoad(this.catalogApi.getCatalog(), {
      onSuccess: (data) => {
        this.loaded = true;
        this._tafsirSourceOptions.set(data.tafsirSources.map(tafsirCatalogItemToOption));
        this._translationSourceOptions.set(
          data.translationSources.map(translationCatalogItemToOption),
        );
        this._fullI3rabSourceOptions.set(data.fullI3rabSources.map(fullI3rabCatalogItemToOption));
      },
      onSettled: () => {
        this.loading = false;
      },
      emptyMessage: 'تعذّر تحميل كتالوج مصادر الدراسة.',
      notFoundMessage: 'تعذّر تحميل كتالوج مصادر الدراسة.',
      connectionMessage: 'تعذّر الاتصال بالخادم.',
    });
  }
}
