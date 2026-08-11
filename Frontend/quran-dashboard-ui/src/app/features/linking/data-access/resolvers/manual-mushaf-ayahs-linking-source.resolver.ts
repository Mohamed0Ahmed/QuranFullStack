import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { manualMushafVerseKeys } from '../../utils/manual-link-shape';
import { ManualMushafAyahReader } from '../manual-mushaf-ayah.reader';

@Injectable({ providedIn: 'root' })
export class ManualMushafAyahsLinkingSourceResolver {
  private readonly reader = inject(ManualMushafAyahReader);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'manual-mushaf-ayahs' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    const verseKeys = manualMushafVerseKeys(source);
    onProgress({ loaded: 0, total: verseKeys.length });
    return this.reader.readCompleteAyahs(verseKeys).pipe(
      map((ayahs) => {
        onProgress({ loaded: ayahs.length, total: verseKeys.length });
        return ayahs;
      }),
    );
  }
}
