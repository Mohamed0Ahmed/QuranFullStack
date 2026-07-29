import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * Section tabs (plan-slice-b.md T415): «كل الأبواب» + one tab per real section,
 * composing `qd-tabs`/`qdTab`. **No** «الأبواب الرئيسية» tab (plan.md §5.1
 * deletion list, contract `:207-211`). Search input and the tree/cards view toggle are
 * deliberately not built here — they would have no working target until T507 (search
 * predicate wiring) and T502 (the cards view) land, and a control with nothing to do
 * is itself a dead control.
 */
@Component({
  selector: 'qd-abwab-toolbar',
  standalone: true,
  imports: [QdTabsComponent, QdTabDirective],
  templateUrl: './abwab-toolbar.component.html',
  styleUrl: './abwab-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabToolbarComponent {
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly activeSectionId = input<number | null>(null);

  readonly sectionChanged = output<number | null>();

  protected get sectionTabsAriaLabel(): string { return ABWAB_LABELS.sectionTabsAriaLabel; }
  protected get allDoorsTabLabel(): string { return ABWAB_LABELS.allDoorsTab; }

  protected selectSection(sectionId: number | null): void {
    this.sectionChanged.emit(sectionId);
  }
}
