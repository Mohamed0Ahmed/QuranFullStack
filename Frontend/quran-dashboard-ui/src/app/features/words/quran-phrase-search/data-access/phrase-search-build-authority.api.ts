import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseSearchCapabilitiesResponseApiResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response-api-response';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class PhraseSearchBuildAuthorityApi {
  private readonly http = inject(HttpClient);
  private readonly capabilitiesUrl = `${environment.apiBaseUrl}/api/quran/phrase-search/capabilities`;

  getCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.http.get<PhraseSearchCapabilitiesResponseApiResponse>(this.capabilitiesUrl);
  }
}
