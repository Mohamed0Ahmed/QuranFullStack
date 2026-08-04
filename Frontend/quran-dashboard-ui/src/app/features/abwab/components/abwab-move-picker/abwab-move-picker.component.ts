import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabMoveDestination } from '../../models/abwab.models';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';

function subtreeMatches(node: AbwabNode, query: string): boolean {
  return node.name.includes(query) || node.children.some((child) => subtreeMatches(child, query));
}

interface AbwabMovePickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

let nextModalId = 0;

@Component({
  selector: 'qd-abwab-move-picker',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, QdTabsComponent, QdTabDirective],
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

  readonly closed = output<void>();
  readonly confirmed = output<AbwabMoveDestination>();

  private readonly modalId = nextModalId++;
  protected readonly titleId = `abwab-move-picker-title-${this.modalId}`;
  protected readonly destinationsId = `abwab-move-picker-destinations-${this.modalId}`;

  protected readonly pickedSectionId = signal<number | null>(null);
  protected readonly pickedParentId = signal<number | null>(null);
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly searchQuery = signal('');

  protected get descriptionText(): string { return ABWAB_LABELS.movePickerDescription; }
  protected get sectionStripLabel(): string { return ABWAB_LABELS.movePickerSectionStripLabel; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.movePickerSearchPlaceholder; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }
  protected get pickSectionHint(): string { return ABWAB_LABELS.movePickerPickSectionHint; }
  protected get asMainDoorLabel(): string { return ABWAB_LABELS.asMainDoorOption; }
  protected get confirmLabel(): string { return ABWAB_LABELS.moveConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected sectionTabId(sectionId: number): string {
    return `abwab-move-picker-section-${this.modalId}-${sectionId}`;
  }

  protected readonly destinationRows = computed<readonly AbwabMovePickerRow[]>(() => {
    const sectionId = this.pickedSectionId();
    if (sectionId === null) {
      return [];
    }
    const excluded = this.excludedIds();
    const expanded = this.expandedIds();
    const query = this.searchQuery().trim();
    const rows: AbwabMovePickerRow[] = [];

    function walk(node: AbwabNode, depth: number): void {
      if (query !== '' && !subtreeMatches(node, query)) {
        return;
      }
      const children = node.children.filter((child) => !excluded.has(child.id));
      const hasChildren = children.length > 0;
      const isExpanded = expanded.has(node.id) || (query !== '' && hasChildren);
      rows.push({ node, depth, hasChildren, isExpanded });
      if (isExpanded) {
        for (const child of children) {
          walk(child, depth + 1);
        }
      }
    }

    for (const root of this.liveRoots()) {
      if (root.sectionId === sectionId && !excluded.has(root.id)) {
        walk(root, 0);
      }
    }
    return rows;
  });

  private readonly sectionHasDoors = computed(() => {
    const sectionId = this.pickedSectionId();
    const excluded = this.excludedIds();
    return this.liveRoots().some((root) => root.sectionId === sectionId && !excluded.has(root.id));
  });

  protected readonly searchFoundNothing = computed(
    () => this.searchQuery().trim() !== '' && this.sectionHasDoors() && this.destinationRows().length === 0,
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

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
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
    const targetSectionId = this.pickedSectionId();
    if (targetSectionId === null) {
      return;
    }
    this.confirmed.emit({ targetParentId: this.pickedParentId(), targetSectionId });
  }

  protected cancel(): void {
    this.closed.emit();
  }
}
