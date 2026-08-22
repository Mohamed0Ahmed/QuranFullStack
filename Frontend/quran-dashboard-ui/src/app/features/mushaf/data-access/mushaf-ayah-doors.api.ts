import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { MushafAyahDoorsResponse } from '../../../core/api/generated/models';
import { ApiResponse } from '../../../core/data-access/api-response.model';

@Injectable({ providedIn: 'root' })
export class MushafAyahDoorsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getDoors(verseKey: string): Observable<ApiResponse<MushafAyahDoorsResponse>> {
    return this.http.get<ApiResponse<MushafAyahDoorsResponse>>(
      `${this.baseUrl}/api/mushaf/ayahs/${encodeURIComponent(verseKey)}/doors`,
    );
  }
}
