import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafStudySourceCatalogDto } from '../models/mushaf.models';

@Injectable({ providedIn: 'root' })
export class MushafStudySourceCatalogApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getCatalog(): Observable<ApiResponse<MushafStudySourceCatalogDto>> {
    return this.http.get<ApiResponse<MushafStudySourceCatalogDto>>(
      `${this.baseUrl}/api/mushaf/study-sources`,
    );
  }
}
