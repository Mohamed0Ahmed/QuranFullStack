import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceResolverRegistry } from './linking-source-resolver.registry';

export interface LinkingSourceResolveProgress {
  loaded: number;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolver {
  private readonly registry = inject(LinkingSourceResolverRegistry);

  resolve(
    source: LinkingSourceDescriptor,
    onProgress: (progress: LinkingSourceResolveProgress) => void,
  ): Observable<readonly LinkingAyah[]> {
    return this.registry.resolve(source, onProgress);
  }
}
