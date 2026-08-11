import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceResolverRegistry } from './linking-source-resolver.registry';

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolver {
  private readonly registry = inject(LinkingSourceResolverRegistry);

  resolve(
    source: LinkingSourceDescriptor,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return this.registry.resolve(source, onProgress);
  }
}
