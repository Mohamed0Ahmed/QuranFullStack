import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * Section tabs + the tree/cards view toggle + search (plan-slice-b.md T415/T502/T507):
 * «كل الأبواب» + one tab per real section, composing `qd-tabs`/`qdTab`. **No**
 * «الأبواب الرئيسية» tab (plan.md §5.1 deletion list, contract `:207-211`).
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
  readonly view = input<AbwabView>('tree');
  readonly searchQuery = input('');
  /** Item 19 — root doors per section (state/abwab-tree.builder.ts), and the total for «كل
   * الأبواب». Not derivable from `sections` here: `doorsInScopeCount` on the DTO answers a
   * different question (all depths, item 17's shipped stat) — see abwab.labels.ts. */
  readonly rootCountBySectionId = input<ReadonlyMap<number, number>>(new Map());
  readonly totalRootCount = input(0);
  /** The archive view has no live section grouping (plan.md §4.5) — tabs and the
   * tree/cards toggle would be inert controls there, so the caller hides them,
   * keeping only search (matrix M4/M31: search still filters the archive tree). */
  readonly hideSectionControls = input(false);

  readonly sectionChanged = output<number | null>();
  readonly viewChanged = output<AbwabView>();
  readonly searchQueryChanged = output<string>();

  protected get sectionTabsAriaLabel(): string { return ABWAB_LABELS.sectionTabsAriaLabel; }
  protected get allDoorsTabLabel(): string { return ABWAB_LABELS.allDoorsTab; }
  protected get searchLabel(): string { return ABWAB_LABELS.searchLabel; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.searchPlaceholder; }
  protected get treeViewLabel(): string { return ABWAB_LABELS.viewToggleTree; }
  protected get cardsViewLabel(): string { return ABWAB_LABELS.viewToggleCards; }

  protected rootCountFor(sectionId: number): number {
    return this.rootCountBySectionId().get(sectionId) ?? 0;
  }

  protected sectionCountAriaLabel(sectionName: string, sectionId: number): string {
    return ABWAB_LABELS.tabRootCountAriaLabel(sectionName, this.rootCountFor(sectionId));
  }

  protected get allDoorsCountAriaLabel(): string {
    return ABWAB_LABELS.allDoorsTabRootCountAriaLabel(this.totalRootCount());
  }

  protected selectSection(sectionId: number | null): void {
    this.sectionChanged.emit(sectionId);
  }

  protected selectView(view: AbwabView): void {
    this.viewChanged.emit(view);
  }

  protected onSearchInput(event: Event): void {
    this.searchQueryChanged.emit((event.target as HTMLInputElement).value);
  }
}
