import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabMoveDestination } from '../../models/abwab.models';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { searchAbwabNodes } from '../../state/abwab-tree-search';
import { AbwabSearchControlsComponent } from '../abwab-search-controls/abwab-search-controls.component';

interface AbwabMovePickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
  readonly isMatched: boolean;
}

let nextModalId = 0;

@Component({
  selector: 'qd-abwab-move-picker',
  standalone: true,
  imports: [
    AbwabSearchControlsComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdModalShellComponent,
    QdTabsComponent,
    QdTabDirective,
  ],
  templateUrl: './abwab-move-picker.component.html',
  styleUrl: './abwab-move-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabMovePickerComponent {
  readonly open = input(false);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly excludedIds = input<ReadonlySet<number>>(new Set());
  readonly movedSectionIds = input<readonly number[]>([]);
  readonly titleText = input('');
  readonly canConfirm = input(false);
  readonly busy = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly closed = output<void>();
  readonly confirmed = output<AbwabMoveDestination>();

  private readonly modalId = nextModalId++;
  protected readonly destinationsId = `abwab-move-picker-destinations-${this.modalId}`;

  protected readonly pickedSectionId = signal<number | null>(null);
  protected readonly pickedParentId = signal<number | null>(null);
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly searchQuery = signal('');
  protected readonly hideUnrelatedRoots = signal(false);

  protected get descriptionText(): string { return ABWAB_LABELS.movePickerDescription; }
  protected get sectionStripLabel(): string { return ABWAB_LABELS.movePickerSectionStripLabel; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.movePickerSearchPlaceholder; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }
  protected get pickSectionHint(): string { return ABWAB_LABELS.movePickerPickSectionHint; }
  protected get asMainDoorLabel(): string { return ABWAB_LABELS.asMainDoorOption; }
  protected get confirmLabel(): string { return ABWAB_LABELS.moveConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected readonly labels = ABWAB_LABELS;

  protected sectionTabId(sectionId: number): string {
    return `abwab-move-picker-section-${this.modalId}-${sectionId}`;
  }

  private readonly sectionRoots = computed(() => {
    const sectionId = this.pickedSectionId();
    return sectionId === null
      ? []
      : this.liveRoots().filter((root) => root.sectionId === sectionId);
  });
  private readonly searchResult = computed(() => searchAbwabNodes(
    this.sectionRoots(),
    this.searchQuery(),
    {
      hideUnrelatedRoots: this.hideUnrelatedRoots(),
      omittedSubtreeIds: this.excludedIds(),
    },
  ));
  protected readonly searchMatchCount = computed(() => this.searchResult().matchedIds.size);

  protected readonly destinationRows = computed<readonly AbwabMovePickerRow[]>(() => {
    const sectionId = this.pickedSectionId();
    if (sectionId === null) {
      return [];
    }
    const expanded = this.expandedIds();
    const searchExpanded = this.searchResult().autoExpandedIds;
    const matchedIds = this.searchResult().matchedIds;
    const rows: AbwabMovePickerRow[] = [];

    function walk(node: AbwabNode, depth: number): void {
      const children = node.children;
      const hasChildren = children.length > 0;
      const isExpanded = expanded.has(node.id) || searchExpanded.has(node.id);
      rows.push({
        node,
        depth,
        hasChildren,
        isExpanded,
        isMatched: matchedIds.has(node.id),
      });
      if (isExpanded) {
        for (const child of children) {
          walk(child, depth + 1);
        }
      }
    }

    this.searchResult().displayRoots.forEach((root) => walk(root, 0));
    return rows;
  });

  private readonly sectionHasDoors = computed(() => {
    const sectionId = this.pickedSectionId();
    const excluded = this.excludedIds();
    return this.liveRoots().some((root) => root.sectionId === sectionId && !excluded.has(root.id));
  });

  protected readonly searchFoundNothing = computed(
    () => this.searchResult().isFiltering
      && this.hideUnrelatedRoots()
      && this.sectionHasDoors()
      && this.searchResult().displayRoots.length === 0,
  );

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }
      untracked(() => {
        const movedSectionIds = this.movedSectionIds();
        this.pickedSectionId.set(null);
        this.pickedParentId.set(null);
        this.expandedIds.set(new Set());
        this.searchQuery.set('');
        this.hideUnrelatedRoots.set(false);

        const distinct = new Set(movedSectionIds);
        if (distinct.size === 1) {
          this.pickSection([...distinct][0]);
        }
      });
    });
  }

  protected pickSection(sectionId: number): void {
    this.pickedSectionId.set(sectionId);
    this.pickedParentId.set(null);
    this.expandedIds.set(new Set());
  }

  protected expandAriaLabel(row: AbwabMovePickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(row.node.name);
  }

  protected toggleExpanded(event: Event, row: AbwabMovePickerRow): void {
    event.stopPropagation();
    const next = new Set(this.expandedIds());
    if (!next.delete(row.node.id)) {
      next.add(row.node.id);
    }
    this.expandedIds.set(next);
  }

  protected pickAsMain(): void {
    this.pickedParentId.set(null);
  }

  protected pickParent(id: number): void {
    this.pickedParentId.set(id);
  }

  protected confirm(): void {
    if (!this.canConfirm() || this.busy()) {
      return;
    }
    const targetSectionId = this.pickedSectionId();
    if (targetSectionId === null) {
      return;
    }
    this.confirmed.emit({ targetParentId: this.pickedParentId(), targetSectionId });
  }

  protected cancel(): void {
    if (!this.busy()) {
      this.closed.emit();
    }
  }
}
