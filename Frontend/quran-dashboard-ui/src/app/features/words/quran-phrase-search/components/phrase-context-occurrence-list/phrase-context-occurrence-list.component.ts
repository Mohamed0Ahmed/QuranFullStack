import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { PhraseContextOccurrenceDto } from '../../../../../core/api/generated/models/phrase-context-occurrence-dto';
import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { AyahCardComponent } from '../../../../../shared/ui/ayah-card/ayah-card.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../../shared/ui/result-list/result-list.directive';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

interface ContextOccurrenceRow {
  readonly occurrence: PhraseContextOccurrenceDto;
  readonly mushafTarget: DetailOverlayBaseTarget;
}

@Component({
  selector: 'qd-phrase-context-occurrence-list',
  standalone: true,
  imports: [
    AyahCardComponent,
    DetailOverlayAyahLinkDirective,
    PhraseHighlightedAyahComponent,
    QdResultItemDirective,
    QdResultListDirective,
  ],
  templateUrl: './phrase-context-occurrence-list.component.html',
  styleUrl: './phrase-context-occurrence-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextOccurrenceListComponent {
  readonly items = input.required<readonly PhraseContextOccurrenceDto[]>();
  readonly totalCount = input.required<number>();
  readonly nextCursor = input<string | null>(null);
  readonly busy = input(false);

  readonly closeRequested = output<void>();
  readonly moreRequested = output<void>();

  protected readonly rows = computed<readonly ContextOccurrenceRow[]>(() =>
    this.items().map((occurrence) => {
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
}
