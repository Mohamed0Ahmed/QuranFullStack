import { Injectable, inject, signal } from '@angular/core';

import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { SourceOption } from '../models/mushaf.models';
import {
  fullI3rabCatalogItemToOption,
  tafsirCatalogItemToOption,
  translationCatalogItemToOption,
} from '../utils/study-source-catalog.labels';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';

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
