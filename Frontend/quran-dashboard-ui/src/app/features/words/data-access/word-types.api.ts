import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeCase,
  WordTypeMainType,
  WordTypeRowDto,
  WordTypeSort,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypeTense,
  WordTypeTreeDto,
  WordTypeVoice,
} from '../models/word-types.models';
import {
  WordTypeGroupedKind,
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedRequestParams,
  WordTypeGroupedSummaryDto,
} from '../models/word-types-detail.models';

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
    search: string | null;
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

    if (options.search) {
      params = params.set('search', options.search);
    }

    return this.http.get<ApiResponse<PagedResultDto<WordTypeRowDto>>>(`${this.baseUrl}/api/words/word-types/words`, { params });
  }

  getTableRows(options: {
    type: string;
    childCode: string | null;
    case: WordTypeCase;
    tense: WordTypeTense;
    voice: WordTypeVoice;
    search: string | null;
    tableView: WordTypeTableView;
    sort: WordTypeSort;
    page: number;
    pageSize: number;
  }): Observable<ApiResponse<PagedResultDto<WordTypeTableRowDto>>> {
    let params = this.identityParams(options)
      .set('type', options.type)
      .set('tableView', options.tableView)
      .set('sort', options.sort)
      .set('page', options.page)
      .set('pageSize', options.pageSize);

    if (options.childCode !== null) {
      params = params.set('childCode', options.childCode);
    }

    if (options.search) {
      params = params.set('search', options.search);
    }

    return this.http.get<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>(`${this.baseUrl}/api/words/word-types/table`, { params });
  }

  getGroupedSummary(options: WordTypeGroupedRequestParams): Observable<ApiResponse<WordTypeGroupedSummaryDto>> {
    return this.http.get<ApiResponse<WordTypeGroupedSummaryDto>>(this.groupedDetailUrl(options), {
      params: this.groupedScopeParams(options),
    });
  }

  getGroupedMemberWords(
    options: WordTypeGroupedRequestParams,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>> {
    const params = this.groupedScopeParams(options)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>>(
      `${this.groupedDetailUrl(options)}/words`,
      { params },
    );
  }

  getGroupedAyahMatches(
    options: WordTypeGroupedRequestParams,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>> {
    const params = this.groupedScopeParams(options)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>>(
      `${this.groupedDetailUrl(options)}/ayahs`,
      { params },
    );
  }

  getGroupedSurahs(options: WordTypeGroupedRequestParams): Observable<ApiResponse<WordTypeSurahsResponseDto>> {
    return this.http.get<ApiResponse<WordTypeSurahsResponseDto>>(`${this.groupedDetailUrl(options)}/surahs`, {
      params: this.groupedScopeParams(options),
    });
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

  private groupedDetailUrl(options: WordTypeGroupedRequestParams): string {
    return `${this.baseUrl}/api/words/word-types/table/${this.groupedRouteKind(options.kind)}/${options.dimensionId}`;
  }

  private groupedScopeParams(options: WordTypeGroupedRequestParams): HttpParams {
    let params = this.identityParams(options).set('type', options.type);
    if (options.childCode !== null) {
      params = params.set('childCode', options.childCode);
    }
    return params;
  }

  private groupedRouteKind(kind: WordTypeGroupedKind): WordTypeGroupedRouteKind {
    const routeKinds: Record<WordTypeGroupedKind, WordTypeGroupedRouteKind> = {
      root: 'roots',
      stem: 'stems',
      lemma: 'lemmas',
    };
    return routeKinds[kind];
  }
}

export interface WordTypeIdentityParams {
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

type WordTypeGroupedRouteKind = 'roots' | 'stems' | 'lemmas';
