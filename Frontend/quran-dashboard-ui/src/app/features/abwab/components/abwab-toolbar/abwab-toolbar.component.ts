import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabNode, AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabSearchControlsComponent } from '../abwab-search-controls/abwab-search-controls.component';

const SEARCH_RESULTS_MIN_CHARACTERS = 3;

@Component({
  selector: 'qd-abwab-toolbar',
  standalone: true,
  imports: [AbwabSearchControlsComponent, QdActionDirective, QdTabsComponent, QdTabDirective],
  templateUrl: './abwab-toolbar.component.html',
  styleUrl: './abwab-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabToolbarComponent {
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly activeSectionId = input<number | null>(null);
  readonly view = input<AbwabView>('tree');
  readonly searchQuery = input('');
  readonly searchMatchCount = input(0);
  readonly searchResults = input<readonly AbwabNode[]>([]);
  readonly hideUnrelatedRoots = input(false);
  readonly rootCountBySectionId = input<ReadonlyMap<number, number>>(new Map());
  readonly totalRootCount = input(0);
  readonly hideSectionControls = input(false);
  readonly searchOnly = input(false);
  readonly canExpandTree = input(false);
  readonly canCollapseTree = input(false);
  readonly treeExpansionDisabled = input(false);

  readonly sectionChanged = output<number | null>();
  readonly viewChanged = output<AbwabView>();
  readonly searchQueryChanged = output<string>();
  readonly hideUnrelatedRootsChanged = output<boolean>();
  readonly searchResultSelected = output<number>();
  readonly expandAllRequested = output<void>();
  readonly collapseAllRequested = output<void>();

  protected get sectionTabsAriaLabel(): string { return ABWAB_LABELS.sectionTabsAriaLabel; }
  protected get allDoorsTabLabel(): string { return ABWAB_LABELS.allDoorsTab; }
  protected get viewToggleAriaLabel(): string { return ABWAB_LABELS.viewToggleAriaLabel; }
  protected get treeViewLabel(): string { return ABWAB_LABELS.viewToggleTree; }
  protected get cardsViewLabel(): string { return ABWAB_LABELS.viewToggleCards; }
  protected get searchResultsAriaLabel(): string { return ABWAB_LABELS.searchResultsAriaLabel; }
  protected get treeExpansionGroupAriaLabel(): string { return ABWAB_LABELS.treeExpansionGroupAriaLabel; }
  protected get treeExpandAllLabel(): string { return ABWAB_LABELS.treeExpandAll; }
  protected get treeCollapseAllLabel(): string { return ABWAB_LABELS.treeCollapseAll; }
  protected get treeExpansionSearchDisabledHint(): string { return ABWAB_LABELS.treeExpansionSearchDisabledHint; }

  protected readonly showSearchResults = computed(() =>
    !this.hideSectionControls()
    && Array.from(this.searchQuery().trim()).length >= SEARCH_RESULTS_MIN_CHARACTERS
    && this.searchResults().length > 0,
  );

  protected searchResultAriaLabel(doorName: string): string {
    return ABWAB_LABELS.searchResultAriaLabel(doorName);
  }

  protected readonly searchScopeHint = computed(() => {
    if (this.hideSectionControls()) {
      return ABWAB_LABELS.searchScopeHintArchive;
    }
    return this.view() === 'cards'
      ? ABWAB_LABELS.searchScopeHintCards
      : ABWAB_LABELS.searchScopeHintTree;
  });

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

  protected selectSearchResult(doorId: number): void {
    this.searchResultSelected.emit(doorId);
  }

}
