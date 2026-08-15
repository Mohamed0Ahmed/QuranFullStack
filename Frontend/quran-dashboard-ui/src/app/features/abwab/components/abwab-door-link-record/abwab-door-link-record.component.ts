import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { DoorLinkAyahDto } from '../../../../core/api/generated/models/door-link-ayah-dto';
import { DoorLinkRecordSummaryDto } from '../../../../core/api/generated/models/door-link-record-summary-dto';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdRefreshingIndicatorComponent } from '../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { LinkingAyahCardComponent } from '../../../linking/components/linking-ayah-card/linking-ayah-card.component';
import { AbwabDoorLinkExpandedState } from '../../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { toAbwabLinkingAyah } from '../abwab-door-link-editor/abwab-door-link-ayah.mapper';
import { AbwabDoorLinkEditorComponent } from '../abwab-door-link-editor/abwab-door-link-editor.component';

export interface AbwabDoorLinkAyahEntry {
  readonly ayah: DoorLinkAyahDto;
  readonly position: number;
}

interface AbwabDoorLinkDisplayAyah {
  readonly ayah: ReturnType<typeof toAbwabLinkingAyah>;
  readonly position: number;
}

@Component({
  selector: 'qd-abwab-door-link-record',
  standalone: true,
  imports: [
    LinkingAyahCardComponent,
    AbwabDoorLinkEditorComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdRefreshingIndicatorComponent,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './abwab-door-link-record.component.html',
  styleUrl: './abwab-door-link-record.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkRecordComponent {
  readonly record = input.required<DoorLinkRecordSummaryDto>();
  readonly recordPosition = input.required<number>();
  readonly totalRecords = input.required<number>();
  readonly selected = input(false);
  readonly expanded = input(false);
  readonly expandedState = input<AbwabDoorLinkExpandedState | null>(null);
  readonly ayahEntries = input<readonly AbwabDoorLinkAyahEntry[]>([]);
  readonly hasMoreAyahs = input(false);
  readonly editing = input(false);
  readonly interactionDisabled = input(false);

  readonly selectionToggled = output<number>();
  readonly expansionToggled = output<DoorLinkRecordSummaryDto>();
  readonly retryAyahs = output<void>();
  readonly loadMoreAyahs = output<void>();

  protected readonly displayAyahs = computed<readonly AbwabDoorLinkDisplayAyah[]>(() =>
    this.ayahEntries().map(({ ayah, position }) => ({ ayah: toAbwabLinkingAyah(ayah), position })),
  );
  protected readonly initialLoading = computed(
    () => this.displayAyahs().length === 0 && this.expandedState()?.status === 'loading',
  );
  protected readonly refreshing = computed(() => {
    const status = this.expandedState()?.status;
    return this.displayAyahs().length > 0 && (status === 'loading' || status === 'refreshing');
  });

  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get emptyLabel(): string { return ABWAB_LABELS.doorLinksAyahsEmpty; }
  protected get loadingLabel(): string { return ABWAB_LABELS.doorLinksAyahsLoading; }
  protected get loadMoreLabel(): string { return ABWAB_LABELS.doorLinksLoadMoreAyahs; }
  protected get sourcesLabel(): string { return ABWAB_LABELS.doorLinksSources; }

  protected kindLabel(): string {
    return this.record().isGrouped ? ABWAB_LABELS.doorLinksGrouped : ABWAB_LABELS.doorLinksIndependent;
  }

  protected ayahCountLabel(): string {
    return ABWAB_LABELS.doorLinksAyahCount(this.record().ayahCount);
  }

  protected wordCountLabel(): string {
    return ABWAB_LABELS.doorLinksWordCount(this.record().selectedWordCount);
  }

  protected descriptionCountLabel(): string {
    return ABWAB_LABELS.doorLinksDescriptionCount(this.record().descriptionCount);
  }

  protected expandLabel(): string {
    return this.expanded()
      ? ABWAB_LABELS.doorLinksCollapseRecord(this.recordPosition())
      : ABWAB_LABELS.doorLinksExpandRecord(this.recordPosition());
  }
}
