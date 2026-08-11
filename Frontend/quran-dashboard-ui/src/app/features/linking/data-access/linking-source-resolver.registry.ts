import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceDescriptor, LinkingSourceKind } from '../models/linking-source.models';
import { UniqueWordLinkingSourceResolver } from './resolvers/unique-word-linking-source.resolver';
import { RootLinkingSourceResolver } from './resolvers/root-linking-source.resolver';
import { LemmaLinkingSourceResolver } from './resolvers/lemma-linking-source.resolver';
import { StemLinkingSourceResolver } from './resolvers/stem-linking-source.resolver';
import { WordTypeLinkingSourceResolver } from './resolvers/word-type-linking-source.resolver';

export interface LinkingSourceResolverRegistration {
  readonly kind: LinkingSourceKind;
  resolve(
    source: LinkingSourceDescriptor,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]>;
}

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolverRegistry {
  private readonly uniqueWordResolver = inject(UniqueWordLinkingSourceResolver);
  private readonly rootResolver = inject(RootLinkingSourceResolver);
  private readonly lemmaResolver = inject(LemmaLinkingSourceResolver);
  private readonly stemResolver = inject(StemLinkingSourceResolver);
  private readonly wordTypeResolver = inject(WordTypeLinkingSourceResolver);
  private readonly registrations: ReadonlyMap<LinkingSourceKind, LinkingSourceResolverRegistration> = new Map<
    LinkingSourceKind,
    LinkingSourceResolverRegistration
  >([
    [
      'unique-word',
      {
        kind: 'unique-word',
        resolve: (
          source: LinkingSourceDescriptor,
          onProgress: (progress: { loaded: number; total: number }) => void,
        ) => {
          if (source.kind !== 'unique-word') {
            throw new Error('مصدر الربط غير متوافق مع محلل الآيات.');
          }
          return this.uniqueWordResolver.resolve(source, onProgress);
        },
      },
    ],
    [
      'root',
      {
        kind: 'root',
        resolve: (source, onProgress) => {
          if (source.kind !== 'root') {
            throw new Error('مصدر الربط غير متوافق مع محلل الجذر.');
          }
          return this.rootResolver.resolve(source, onProgress);
        },
      },
    ],
    [
      'lemma',
      {
        kind: 'lemma',
        resolve: (source, onProgress) => {
          if (source.kind !== 'lemma') {
            throw new Error('مصدر الربط غير متوافق مع محلل اللمة.');
          }
          return this.lemmaResolver.resolve(source, onProgress);
        },
      },
    ],
    [
      'stem',
      {
        kind: 'stem',
        resolve: (source, onProgress) => {
          if (source.kind !== 'stem') {
            throw new Error('مصدر الربط غير متوافق مع محلل الصيغة.');
          }
          return this.stemResolver.resolve(source, onProgress);
        },
      },
    ],
    [
      'word-type',
      {
        kind: 'word-type',
        resolve: (source, onProgress) => {
          if (source.kind !== 'word-type') {
            throw new Error('مصدر الربط غير متوافق مع محلل نوع الكلمة.');
          }
          return this.wordTypeResolver.resolve(source, onProgress);
        },
      },
    ],
  ]);

  resolve(
    source: LinkingSourceDescriptor,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    const registration = this.registrations.get(source.kind);
    if (!registration) {
      throw new Error('هذا المصدر غير مدعوم في الربط المباشر بعد.');
    }
    return registration.resolve(source, onProgress);
  }
}
