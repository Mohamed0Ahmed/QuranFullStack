import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { DoorLinkRecordSummaryDto } from '../../../../core/api/generated/models/door-link-record-summary-dto';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdRefreshingIndicatorComponent } from '../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../state/abwab-door-links.facade';
import {
  AbwabDoorLinkAyahEntry,
  AbwabDoorLinkRecordComponent,
} from '../abwab-door-link-record/abwab-door-link-record.component';

interface AbwabDoorLinkRecordEntry {
  readonly record: DoorLinkRecordSummaryDto;
  readonly position: number;
}

@Component({
  selector: 'qd-abwab-door-links-panel',
  standalone: true,
  imports: [
    AbwabDoorLinkRecordComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdRefreshingIndicatorComponent,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './abwab-door-links-panel.component.html',
  styleUrl: './abwab-door-links-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinksPanelComponent {
  protected readonly facade = inject(AbwabDoorLinksFacade);

  readonly doorId = input.required<number>();
  readonly doorName = input.required<string>();

  protected readonly state = this.facade.state;
  protected readonly recordEntries = computed<readonly AbwabDoorLinkRecordEntry[]>(() =>
    Object.values(this.state().records.pages)
      .sort((left, right) => left.page - right.page)
      .flatMap((page) =>
        page.items.map((record, index) => ({
          record,
          position: (page.page - 1) * page.pageSize + index + 1,
        })),
      ),
  );
  protected readonly ayahEntries = computed<readonly AbwabDoorLinkAyahEntry[]>(() => {
    const expanded = this.state().expanded;
    if (expanded === null) {
      return [];
    }
    return Object.values(expanded.pages)
      .sort((left, right) => left.page - right.page)
      .flatMap((page) =>
        page.items.map((ayah, index) => ({
          ayah,
          position: (page.page - 1) * page.pageSize + index + 1,
        })),
      );
  });
  protected readonly activePage = computed(() =>
    Math.max(...Object.keys(this.state().records.pages).map(Number), 1),
  );
  protected readonly initialLoading = computed(
    () => this.recordEntries().length === 0 && this.state().records.status === 'loading',
  );
  protected readonly refreshing = computed(() =>
    this.recordEntries().length > 0 && ['loading', 'refreshing'].includes(this.state().records.status),
  );

  protected get heading(): string { return ABWAB_LABELS.doorLinksHeading; }
  protected get selectPageLabel(): string { return ABWAB_LABELS.doorLinksSelectPage; }
  protected get selectAllLabel(): string { return ABWAB_LABELS.doorLinksSelectAll; }
  protected get clearSelectionLabel(): string { return ABWAB_LABELS.doorLinksClearSelection; }
  protected get emptyLabel(): string { return ABWAB_LABELS.doorLinksEmpty; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadMoreLabel(): string { return ABWAB_LABELS.doorLinksLoadMore; }
  protected get loadingLabel(): string { return ABWAB_LABELS.doorLinksLoading; }
  protected get dismissLabel(): string { return ABWAB_LABELS.relationsCloseButton; }

  protected totalCountLabel(): string {
    return ABWAB_LABELS.doorLinksRecordsCount(this.state().records.totalCount);
  }

  protected selectedCountLabel(): string {
    return ABWAB_LABELS.doorLinksSelectedCount(this.facade.selectedCount());
  }

  protected isSelected(unitId: number): boolean {
    const selection = this.state().selection;
    const listed = selection.unitIds.includes(unitId);
    return selection.mode === 'only' ? listed : !listed;
  }
}
