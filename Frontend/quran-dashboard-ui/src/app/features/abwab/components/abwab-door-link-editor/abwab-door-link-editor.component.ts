import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { DoorLinkRecordSummaryDto } from '../../../../core/api/generated/models/door-link-record-summary-dto';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { LinkingAyahCardComponent } from '../../../linking/components/linking-ayah-card/linking-ayah-card.component';
import { LinkingAyah } from '../../../linking/models/linking-ayah.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../state/abwab-door-links.facade';
import { toAbwabLinkingAyah } from './abwab-door-link-ayah.mapper';
import { ABWAB_DOOR_LINK_EDITOR_LABELS } from './abwab-door-link-editor.labels';

interface AbwabDoorLinkEditableAyah {
  readonly ayah: LinkingAyah;
  readonly ayahId: number;
  readonly position: number;
}

@Component({
  selector: 'qd-abwab-door-link-editor',
  standalone: true,
  imports: [
    LinkingAyahCardComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './abwab-door-link-editor.component.html',
  styleUrl: './abwab-door-link-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkEditorComponent {
  protected readonly facade = inject(AbwabDoorLinksFacade);

  readonly record = input.required<DoorLinkRecordSummaryDto>();

  protected readonly state = this.facade.state;
  protected readonly edit = computed(() => this.state().edit);
  protected readonly editableAyahs = computed<readonly AbwabDoorLinkEditableAyah[]>(() => {
    const edit = this.edit();
    return edit.ayahs.map((ayah, index) => ({
      ayah: toAbwabLinkingAyah(ayah),
      ayahId: ayah.ayahId,
      position: index + 1,
    }));
  });
  protected readonly selectedWordCount = computed(() =>
    this.edit().ayahs.reduce((count, ayah) => count + ayah.selectedWordIds.length, 0),
  );
  protected readonly preparing = computed(() => this.edit().status === 'preparing');
  protected readonly saving = computed(() => this.edit().status === 'saving');
  protected readonly canSave = computed(() => ['ready', 'save-error'].includes(this.edit().status));

  protected get heading(): string { return ABWAB_DOOR_LINK_EDITOR_LABELS.heading; }
  protected get preparingLabel(): string { return ABWAB_DOOR_LINK_EDITOR_LABELS.preparing; }
  protected get emptyLabel(): string { return ABWAB_LABELS.doorLinksAyahsEmpty; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get saveLabel(): string { return ABWAB_DOOR_LINK_EDITOR_LABELS.save; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected selectedCountLabel(): string {
    return ABWAB_LABELS.doorLinksWordCount(this.selectedWordCount());
  }

  protected kindLabel(): string {
    return this.record().isGrouped ? ABWAB_LABELS.doorLinksGrouped : ABWAB_LABELS.doorLinksIndependent;
  }

  protected toggleWord(ayahId: number, quranWordId: number): void {
    const selected = this.edit().ayahs
      .find((ayah) => ayah.ayahId === ayahId)
      ?.selectedWordIds.includes(quranWordId) ?? false;
    this.facade.setEditWord(ayahId, quranWordId, !selected);
  }
}
