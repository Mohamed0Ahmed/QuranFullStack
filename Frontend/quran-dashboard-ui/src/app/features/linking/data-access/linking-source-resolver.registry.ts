import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceDescriptor, LinkingSourceKind } from '../models/linking-source.models';
import { UniqueWordLinkingSourceResolver } from './resolvers/unique-word-linking-source.resolver';
import { MushafWordLinkingSourceResolver } from './resolvers/mushaf-word-linking-source.resolver';

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
  private readonly mushafWordResolver = inject(MushafWordLinkingSourceResolver);
  private readonly registrations: ReadonlyMap<LinkingSourceKind, LinkingSourceResolverRegistration> = new Map<
    LinkingSourceKind,
    LinkingSourceResolverRegistration
  >([
    [
      'mushaf-word',
      {
        kind: 'mushaf-word',
        resolve: (
          source: LinkingSourceDescriptor,
          onProgress: (progress: { loaded: number; total: number }) => void,
        ) => {
          if (source.kind !== 'mushaf-word') {
            throw new Error('مصدر الربط غير متوافق مع محلل كلمة المصحف.');
          }
          return this.mushafWordResolver.resolve(source, onProgress);
        },
      },
    ],
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
