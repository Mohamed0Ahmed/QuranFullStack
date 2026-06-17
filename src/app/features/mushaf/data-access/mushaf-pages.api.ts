import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafPageDto } from '../models/mushaf.models';

/**
 * Data-access service for the Mushaf page endpoint. Builds URLs from the
 * HTTPS backend base URL and returns the raw `ApiResponse<T>`. The facade maps
 * it into page-ready view models; this service owns no state.
 *
 * Phase 2 shell: `getPage` is implemented by US1 (T024).
 */
@Injectable({ providedIn: 'root' })
export class MushafPagesApi {
  private readonly baseUrl = environment.apiBaseUrl;

  getPage(pageNumber: number): Observable<ApiResponse<MushafPageDto>> {
    // Implemented in T024 (US1). Returns the page read model.
    void this.baseUrl;
    throw new Error('MushafPagesApi.getPage not implemented — see task T024.');
  }
}
