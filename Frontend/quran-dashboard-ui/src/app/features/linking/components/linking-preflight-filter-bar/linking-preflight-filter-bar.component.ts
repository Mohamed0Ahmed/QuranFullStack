import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { LinkingPreflightCountsDto } from '../../../../core/api/generated/models/linking-preflight-counts-dto';
import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { LINKING_LABELS } from '../../models/linking.labels';

export type LinkingPreflightAyahFilter =
  | 'ALL'
  | 'NEW_AYAH'
  | 'OVERLAP_OTHER_SOURCE'
  | 'UNCHANGED'
  | 'UPDATE';

interface LinkingPreflightFilterOption {
  value: LinkingPreflightAyahFilter;
  label: string;
  count: number;
}

@Component({
  selector: 'qd-linking-preflight-filter-bar',
  standalone: true,
  imports: [QdChipComponent],
  templateUrl: './linking-preflight-filter-bar.component.html',
  styleUrl: './linking-preflight-filter-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightFilterBarComponent {
  readonly counts = input.required<LinkingPreflightCountsDto>();
  readonly selectedFilter = input.required<LinkingPreflightAyahFilter>();
  readonly filterChanged = output<LinkingPreflightAyahFilter>();

  protected readonly labels = LINKING_LABELS;
  protected readonly filters = computed<readonly LinkingPreflightFilterOption[]>(() => {
    const counts = this.counts();
    return [
      { value: 'ALL', label: this.labels.preflightAyahFilterAll, count: counts.requested },
      { value: 'NEW_AYAH', label: this.labels.ayahClassifications.NEW_AYAH, count: counts.new },
      {
        value: 'OVERLAP_OTHER_SOURCE',
        label: this.labels.ayahClassifications.OVERLAP_OTHER_SOURCE,
        count: counts.overlapping,
      },
      { value: 'UNCHANGED', label: this.labels.ayahClassifications.UNCHANGED, count: counts.unchanged },
      {
        value: 'UPDATE',
        label: this.labels.ayahClassifications.UPDATE,
        count: counts.updated,
      },
    ];
  });

  protected selectFilter(filter: LinkingPreflightAyahFilter): void {
    this.filterChanged.emit(filter);
  }
}
