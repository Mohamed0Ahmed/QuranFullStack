import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AyahStudyDto, MushafReaderSources } from '../models/mushaf.models';

export type AyahStudySourceParams = Pick<
  MushafReaderSources,
  'tafsirSource' | 'translationSource' | 'fullI3rabSource'
>;

@Injectable({ providedIn: 'root' })
export class MushafAyahStudyApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getAyahStudy(
    verseKey: string,
    sources: AyahStudySourceParams,
  ): Observable<ApiResponse<AyahStudyDto>> {
    let params = new HttpParams();
    if (sources.tafsirSource) {
      params = params.set('tafsirSource', sources.tafsirSource);
    }
    if (sources.translationSource) {
      params = params.set('translationSource', sources.translationSource);
    }
    if (sources.fullI3rabSource) {
      params = params.set('fullI3rabSource', sources.fullI3rabSource);
    }

    return this.http.get<ApiResponse<AyahStudyDto>>(
      `${this.baseUrl}/api/mushaf/ayahs/${encodeURIComponent(verseKey)}/study`,
      { params },
    );
  }
}
