import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';

import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/** Long enough that a typed word announces once, short enough to feel like a response. */
const ANNOUNCE_SETTLE_MS = 500;

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
  readonly searchMatchCount = input(0);
  readonly rootCountBySectionId = input<ReadonlyMap<number, number>>(new Map());
  readonly totalRootCount = input(0);
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
