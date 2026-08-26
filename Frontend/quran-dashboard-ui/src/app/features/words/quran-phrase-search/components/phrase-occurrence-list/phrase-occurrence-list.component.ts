import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { PhraseOccurrenceDto } from '../../../../../core/api/generated/models/phrase-occurrence-dto';
import { PhraseOccurrencePageResponse } from '../../../../../core/api/generated/models/phrase-occurrence-page-response';
import { AyahCardComponent } from '../../../../../shared/ui/ayah-card/ayah-card.component';
import { PaginationComponent } from '../../../../../shared/ui/pagination/pagination.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../../shared/ui/result-list/result-list.directive';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

interface PhraseOccurrenceRow {
  readonly occurrence: PhraseOccurrenceDto;
  readonly mushafTarget: DetailOverlayBaseTarget;
}

@Component({
  selector: 'qd-phrase-occurrence-list',
  standalone: true,
  imports: [
    AyahCardComponent,
    DetailOverlayAyahLinkDirective,
    PaginationComponent,
    PhraseHighlightedAyahComponent,
    QdResultItemDirective,
    QdResultListDirective,
  ],
  templateUrl: './phrase-occurrence-list.component.html',
  styleUrl: './phrase-occurrence-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseOccurrenceListComponent {
  readonly response = input.required<PhraseOccurrencePageResponse>();
  readonly disabled = input(false);

  readonly closeRequested = output<void>();
  readonly pageChange = output<number>();

  protected readonly rows = computed<readonly PhraseOccurrenceRow[]>(() =>
    this.response().items.map((occurrence) => {
      const deepLink = buildMushafDeepLink({
        pageNumber: occurrence.pageFrom,
        ayah: occurrence.verseKey,
        focusAyah: occurrence.verseKey,
        panel: 'ayah',
      });
      return {
        occurrence,
        mushafTarget: { basePath: deepLink.path, queryParams: deepLink.queryParams },
      };
    }),
  );

  protected position(index: number): number {
    return (this.response().page - 1) * this.response().pageSize + index + 1;
  }

  protected pageLabel(occurrence: PhraseOccurrenceDto): string {
    return occurrence.pageFrom === occurrence.pageTo
      ? `صفحة ${occurrence.pageFrom}`
      : `صفحتا ${occurrence.pageFrom} و${occurrence.pageTo}`;
  }
}
