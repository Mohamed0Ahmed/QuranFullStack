import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AyahStudyDto, MushafReaderSources } from '../models/mushaf.models';

/**
 * Data-access service for the ayah-study endpoint. Loads the three selected
 * source kinds (tafsir, translation, full i3rab) together.
 *
 * Phase 2 shell: `getAyahStudy` is implemented by US3 (T033).
 */
@Injectable({ providedIn: 'root' })
export class MushafAyahStudyApi {
  private readonly baseUrl = environment.apiBaseUrl;

  getAyahStudy(
    verseKey: string,
    sources: Pick<MushafReaderSources, 'tafsirSource' | 'translationSource' | 'fullI3rabSource'>,
  ): Observable<ApiResponse<AyahStudyDto>> {
    // Implemented in T033 (US3). Returns core ayah + the three sources together.
    void this.baseUrl;
    void verseKey;
    void sources;
    throw new Error('MushafAyahStudyApi.getAyahStudy not implemented — see task T033.');
  }
}
