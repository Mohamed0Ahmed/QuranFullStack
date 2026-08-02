import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';

import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/** Long enough that a typed word announces once, short enough to feel like a response. */
const ANNOUNCE_SETTLE_MS = 500;

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
  /** How many doors the current query matched — in the tree these are the marked rows, in
   * cards/archive the ones the filter kept, so the number is honest in every view. */
  readonly searchMatchCount = input(0);
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

  protected readonly matchCountText = computed(() => ABWAB_LABELS.searchMatchCount(this.searchMatchCount()));

  /** What the status region currently holds. Empty except in the settled window below. */
  protected readonly announcedCountText = signal('');

  private announceTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // A `role="status"` bound straight to the count would speak once per typed character. The
    // visible number stays live; the announcement waits for the typing to stop. Debouncing the
    // announcement only — the URL write stays per keystroke, which is a separate open decision.
    effect(() => {
      const query = this.searchQuery();
      const count = this.searchMatchCount();
      untracked(() => {
        this.clearAnnounceTimer();
        if (query === '') {
          // Clearing announces nothing, and emptying now stops a stale count being re-read later.
          this.announcedCountText.set('');
          return;
        }
        this.announceTimer = setTimeout(() => {
          this.announcedCountText.set(ABWAB_LABELS.searchMatchCount(count));
          this.announceTimer = null;
        }, ANNOUNCE_SETTLE_MS);
      });
    });

    // Or a navigation away mid-typing announces into a destroyed view.
    inject(DestroyRef).onDestroy(() => this.clearAnnounceTimer());
  }

  private clearAnnounceTimer(): void {
    if (this.announceTimer !== null) {
      clearTimeout(this.announceTimer);
      this.announceTimer = null;
    }
  }

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
