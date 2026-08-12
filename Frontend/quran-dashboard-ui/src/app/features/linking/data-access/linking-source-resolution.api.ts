import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingResolvedSourceDto } from '../../../core/api/generated/models/linking-resolved-source-dto';
import { LinkingSourceDescriptorBody } from '../../../core/api/generated/models/linking-source-descriptor-body';
import { LinkingWordTypeSelectionBody } from '../../../core/api/generated/models/linking-word-type-selection-body';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingSourceDescriptor, LinkingWordTypeSelection } from '../models/linking-source.models';
import { manualMushafVerseKeys } from '../utils/manual-link-shape';

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolutionApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  resolveSource(source: LinkingSourceDescriptor): Observable<ApiResponse<LinkingResolvedSourceDto>> {
    return this.http.post<ApiResponse<LinkingResolvedSourceDto>>(
      `${this.baseUrl}/api/linking/sources/resolve`,
      toDescriptorBody(source),
    );
  }
}

function toDescriptorBody(source: LinkingSourceDescriptor): LinkingSourceDescriptorBody {
  const body: LinkingSourceDescriptorBody = {
    kind: source.kind,
    label: source.label,
    lemmaId: null,
    manualAyahs: null,
    mode: null,
    rootId: null,
    selection: null,
    stemId: null,
    typeCode: null,
    wordId: null,
  };

  switch (source.kind) {
    case 'manual-mushaf-ayahs':
      return {
        ...body,
        manualAyahs: manualMushafVerseKeys(source).map((verseKey) => ({ verseKey })),
      };
    case 'unique-word':
      return { ...body, mode: source.mode, wordId: source.wordId };
    case 'root':
      return { ...body, rootId: source.rootId };
    case 'lemma':
      return { ...body, lemmaId: source.lemmaId, typeCode: source.typeCode };
    case 'stem':
      return { ...body, stemId: source.stemId, typeCode: source.typeCode };
    case 'word-type':
      return { ...body, selection: toSelectionBody(source.selection) };
  }
}

function toSelectionBody(selection: LinkingWordTypeSelection): LinkingWordTypeSelectionBody {
  const body: LinkingWordTypeSelectionBody = {
    case: null,
    contextCode: null,
    kind: selection.kind,
    lemmaId: null,
    rootId: null,
    scope: {
      case: selection.scope.case,
      childCode: selection.scope.childCode,
      tense: selection.scope.tense,
      type: selection.scope.type,
      voice: selection.scope.voice,
    },
    stemId: null,
    tashkeelWordId: null,
    tense: null,
    voice: null,
  };

  switch (selection.kind) {
    case 'word':
      return {
        ...body,
        case: selection.case,
        contextCode: selection.contextCode,
        tashkeelWordId: selection.tashkeelWordId,
        tense: selection.tense,
        voice: selection.voice,
      };
    case 'root':
      return { ...body, rootId: selection.rootId };
    case 'stem':
      return { ...body, stemId: selection.stemId };
    case 'lemma':
      return { ...body, lemmaId: selection.lemmaId };
  }
}
