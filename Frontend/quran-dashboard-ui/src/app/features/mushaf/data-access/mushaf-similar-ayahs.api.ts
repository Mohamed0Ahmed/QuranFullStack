import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { SimilarAyahsDto } from '../models/mushaf.models';
import type { QuranVerseKey } from '../../../shared/quran/quran-location';

@Injectable({ providedIn: 'root' })
export class MushafSimilarAyahsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getSimilarAyahs(verseKey: QuranVerseKey): Observable<ApiResponse<SimilarAyahsDto>> {
    return this.http.get<ApiResponse<SimilarAyahsDto>>(
      `${this.baseUrl}/api/mushaf/ayahs/${encodeURIComponent(verseKey)}/similar-ayahs`,
    );
  }
}
