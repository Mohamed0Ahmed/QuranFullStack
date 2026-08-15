import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

const ANNOUNCE_SETTLE_MS = 500;
const SEARCH_SETTLE_MS = 180;

@Component({
  selector: 'qd-abwab-toolbar',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective, QdFormFieldComponent, QdTabsComponent, QdTabDirective],
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
  protected get viewToggleAriaLabel(): string { return ABWAB_LABELS.viewToggleAriaLabel; }
  protected get treeViewLabel(): string { return ABWAB_LABELS.viewToggleTree; }
  protected get cardsViewLabel(): string { return ABWAB_LABELS.viewToggleCards; }

  protected readonly matchCountText = computed(() => ABWAB_LABELS.searchMatchCount(this.searchMatchCount()));

  protected readonly searchScopeHint = computed(() => {
    if (this.hideSectionControls()) {
      return ABWAB_LABELS.searchScopeHintArchive;
    }
    return this.view() === 'cards'
      ? ABWAB_LABELS.searchScopeHintCards
      : ABWAB_LABELS.searchScopeHintTree;
  });

  protected readonly announcedCountText = signal('');
  protected readonly searchDraft = signal('');

  private announceTimer: ReturnType<typeof setTimeout> | null = null;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const query = this.searchQuery();
      untracked(() => {
        this.clearSearchTimer();
        this.searchDraft.set(query);
      });
    });

    effect(() => {
      const query = this.searchQuery();
      const count = this.searchMatchCount();
      untracked(() => {
        this.clearAnnounceTimer();
        if (query === '') {
          this.announcedCountText.set('');
          return;
        }
        this.announceTimer = setTimeout(() => {
          this.announcedCountText.set(ABWAB_LABELS.searchMatchCount(count));
          this.announceTimer = null;
        }, ANNOUNCE_SETTLE_MS);
      });
    });

    inject(DestroyRef).onDestroy(() => {
      this.clearAnnounceTimer();
      this.clearSearchTimer();
    });
  }

  private clearAnnounceTimer(): void {
    if (this.announceTimer !== null) {
      clearTimeout(this.announceTimer);
      this.announceTimer = null;
    }
  }

  private clearSearchTimer(): void {
    if (this.searchTimer !== null) {
      clearTimeout(this.searchTimer);
      this.searchTimer = null;
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
    const query = (event.target as HTMLInputElement).value;
    this.searchDraft.set(query);
    this.clearSearchTimer();
    if (query === this.searchQuery()) {
      return;
    }
    this.searchTimer = setTimeout(() => {
      this.searchTimer = null;
      this.searchQueryChanged.emit(query);
    }, SEARCH_SETTLE_MS);
  }
}
