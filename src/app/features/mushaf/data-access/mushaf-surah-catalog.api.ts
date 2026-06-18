import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafSurahCatalogDto } from '../models/mushaf.models';

@Injectable({ providedIn: 'root' })
export class MushafSurahCatalogApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getCatalog(): Observable<ApiResponse<MushafSurahCatalogDto>> {
    return this.http.get<ApiResponse<MushafSurahCatalogDto>>(`${this.baseUrl}/api/mushaf/surahs`);
  }
}
