import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordAnalysisDto } from '../models/mushaf.models';

/**
 * Data-access service for the word-analysis endpoint.
 *
 * Phase 2 shell: `getWordAnalysis` is implemented by US4 (T043).
 */
@Injectable({ providedIn: 'root' })
export class MushafWordAnalysisApi {
  private readonly baseUrl = environment.apiBaseUrl;

  getWordAnalysis(wordLocation: string): Observable<ApiResponse<WordAnalysisDto>> {
    // Implemented in T043 (US4). Returns occurrence + identity + morphology + segments.
    void this.baseUrl;
    void wordLocation;
    throw new Error('MushafWordAnalysisApi.getWordAnalysis not implemented — see task T043.');
  }
}
