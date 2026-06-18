import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafPageDto } from '../models/mushaf.models';

@Injectable({ providedIn: 'root' })
export class MushafPagesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getPage(pageNumber: number): Observable<ApiResponse<MushafPageDto>> {
    return this.http.get<ApiResponse<MushafPageDto>>(
      `${this.baseUrl}/api/mushaf/pages/${pageNumber}`,
    );
  }
}
