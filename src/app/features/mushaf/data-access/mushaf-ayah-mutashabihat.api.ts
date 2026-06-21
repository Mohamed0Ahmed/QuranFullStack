import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AyahMutashabihatDto } from '../models/mushaf.models';

@Injectable({ providedIn: 'root' })
export class MushafAyahMutashabihatApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getAyahMutashabihat(verseKey: string): Observable<ApiResponse<AyahMutashabihatDto>> {
    return this.http.get<ApiResponse<AyahMutashabihatDto>>(
      `${this.baseUrl}/api/mushaf/ayahs/${encodeURIComponent(verseKey)}/mutashabihat`,
    );
  }
}
