import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeCase,
  WordTypeRowDto,
  WordTypeSort,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
  WordTypeTense,
  WordTypeTreeDto,
  WordTypeVoice,
} from '../models/word-types.models';

@Injectable({ providedIn: 'root' })
export class WordTypesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getTree(): Observable<ApiResponse<WordTypeTreeDto>> {
    return this.http.get<ApiResponse<WordTypeTreeDto>>(`${this.baseUrl}/api/words/word-types/tree`);
  }

  getRows(options: {
    type: string;
    childCode: string | null;
    case: WordTypeCase;
    tense: WordTypeTense;
    voice: WordTypeVoice;
    sort: WordTypeSort;
    page: number;
    pageSize: number;
  }): Observable<ApiResponse<PagedResultDto<WordTypeRowDto>>> {
    let params = this.identityParams(options)
      .set('type', options.type)
      .set('sort', options.sort)
      .set('page', options.page)
      .set('pageSize', options.pageSize);

    if (options.childCode !== null) {
      params = params.set('childCode', options.childCode);
    }

    return this.http.get<ApiResponse<PagedResultDto<WordTypeRowDto>>>(`${this.baseUrl}/api/words/word-types/words`, { params });
  }

  getSummary(identity: WordTypeIdentityParams): Observable<ApiResponse<WordTypeSummaryDto>> {
    return this.http.get<ApiResponse<WordTypeSummaryDto>>(
      `${this.baseUrl}/api/words/word-types/words/${identity.tashkeelWordId}`,
      { params: this.identityParams(identity).set('contextCode', identity.contextCode) },
    );
  }

  getAyahMatches(identity: WordTypeIdentityParams, page: number, pageSize: number): Observable<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>> {
    const params = this.identityParams(identity)
      .set('contextCode', identity.contextCode)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>>(
      `${this.baseUrl}/api/words/word-types/words/${identity.tashkeelWordId}/ayahs`,
      { params },
    );
  }

  getSurahs(identity: WordTypeIdentityParams): Observable<ApiResponse<WordTypeSurahsResponseDto>> {
    return this.http.get<ApiResponse<WordTypeSurahsResponseDto>>(
      `${this.baseUrl}/api/words/word-types/words/${identity.tashkeelWordId}/surahs`,
      { params: this.identityParams(identity).set('contextCode', identity.contextCode) },
    );
  }

  private identityParams(options: Partial<Pick<WordTypeIdentityParams, 'case' | 'tense' | 'voice'>>): HttpParams {
    let params = new HttpParams();
    if (options.case && options.case !== 'all') params = params.set('case', options.case);
    if (options.tense && options.tense !== 'all') params = params.set('tense', options.tense);
    if (options.voice && options.voice !== 'all') params = params.set('voice', options.voice);
    return params;
  }
}

export interface WordTypeIdentityParams {
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}
