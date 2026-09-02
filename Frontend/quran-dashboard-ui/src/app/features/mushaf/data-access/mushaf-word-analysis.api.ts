import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordAnalysisDto } from '../models/mushaf.models';
import type { QuranWordLocation } from '../../../shared/quran/quran-location';

@Injectable({ providedIn: 'root' })
export class MushafWordAnalysisApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getWordAnalysis(wordLocation: QuranWordLocation): Observable<ApiResponse<WordAnalysisDto>> {
    return this.http.get<ApiResponse<WordAnalysisDto>>(
      `${this.baseUrl}/api/mushaf/words/${encodeURIComponent(wordLocation)}/analysis`,
    );
  }
}
