import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { DoorLinkAyahDto } from '../../../../core/api/generated/models/door-link-ayah-dto';
import { DoorLinkRecordSummaryDto } from '../../../../core/api/generated/models/door-link-record-summary-dto';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { LinkingAyahCardComponent } from '../../../linking/components/linking-ayah-card/linking-ayah-card.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { toAbwabLinkingAyah } from '../abwab-door-link-editor/abwab-door-link-ayah.mapper';
import { AbwabDoorLinkEditorComponent } from '../abwab-door-link-editor/abwab-door-link-editor.component';

@Component({
  selector: 'qd-abwab-door-link-record',
  standalone: true,
  imports: [
    LinkingAyahCardComponent,
    AbwabDoorLinkEditorComponent,
    QdEmptyStateComponent,
  ],
  templateUrl: './abwab-door-link-record.component.html',
  styleUrl: './abwab-door-link-record.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkRecordComponent {
  readonly record = input.required<DoorLinkRecordSummaryDto>();
  readonly recordPosition = input.required<number>();
  readonly totalRecords = input.required<number>();
  readonly ayahs = input.required<readonly DoorLinkAyahDto[]>();
  readonly selected = input(false);
  readonly editing = input(false);
  readonly selectable = input(false);
  readonly interactionDisabled = input(false);

  readonly selectionToggled = output<number>();

  protected readonly displayAyahs = computed<readonly ReturnType<typeof toAbwabLinkingAyah>[]>(() =>
    this.ayahs().map((ayah) => toAbwabLinkingAyah(ayah)),
  );

  protected get emptyLabel(): string { return ABWAB_LABELS.doorLinksAyahsEmpty; }

  protected kindLabel(): string {
    return this.record().isGrouped ? ABWAB_LABELS.doorLinksGrouped : ABWAB_LABELS.doorLinksIndependent;
  }

  protected toggleSelection(): void {
    if (this.selectable() && !this.interactionDisabled()) {
      this.selectionToggled.emit(this.record().unitId);
    }
  }

}
