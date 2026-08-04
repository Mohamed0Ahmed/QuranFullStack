import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';

import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export type AbwabDoorPickerStatus = 'ready' | 'loading' | 'error' | 'empty';

let nextPickerId = 0;

interface AbwabDoorPickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
  readonly isDisabled: boolean;
  readonly isExcluded: boolean;
}

function subtreeMatches(node: AbwabNode, query: string): boolean {
  return node.name.includes(query) || node.children.some((child) => subtreeMatches(child, query));
}

@Component({
  selector: 'qd-abwab-door-picker',
  standalone: true,
  imports: [QdSkeletonRowsComponent, QdStateComponent],
  templateUrl: './abwab-door-picker.component.html',
  styleUrl: './abwab-door-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorPickerComponent {
  readonly nodes = input.required<readonly AbwabNode[]>();
  readonly pickedIds = input.required<readonly number[]>();
  readonly excludedIds = input<readonly number[]>([]);
  readonly disabledIds = input<readonly number[]>([]);
  readonly disabledTag = input('');
  readonly excludedTag = input('');
  readonly single = input(false);
  readonly status = input<AbwabDoorPickerStatus>('ready');
  readonly errorMessage = input('');
  readonly emptyMessage = input.required<string>();
  readonly searchPlaceholder = input.required<string>();
  readonly testIdPrefix = input.required<string>();

  readonly toggled = output<number>();
  readonly retry = output<void>();

  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  protected readonly searchQuery = signal('');
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly collapsedExcludedIds = signal<ReadonlySet<number>>(new Set());

  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }

  protected readonly pickerName = `abwab-door-picker-${nextPickerId++}`;

  protected readonly doorsErrorMessage = computed(() => (this.status() === 'error' ? this.errorMessage() : ''));

  protected readonly searchFoundNothing = computed(
    () => this.searchQuery().trim() !== '' && this.nodes().length > 0,
  );

  private readonly pickedSet = computed(() => new Set(this.pickedIds()));

  protected readonly rows = computed<readonly AbwabDoorPickerRow[]>(() => {
    const query = this.searchQuery().trim();
    const excluded = new Set(this.excludedIds());
    const disabled = new Set(this.disabledIds());
    const expanded = this.expandedIds();
    const collapsedExcluded = this.collapsedExcludedIds();
    const rows: AbwabDoorPickerRow[] = [];

    const walk = (node: AbwabNode, depth: number): void => {
      if (query !== '' && !subtreeMatches(node, query)) {
        return;
      }
      const hasChildren = node.children.length > 0;
      const isExcluded = excluded.has(node.id);
      const defaultExpanded = isExcluded ? !collapsedExcluded.has(node.id) : expanded.has(node.id);
      const isExpanded = defaultExpanded || (query !== '' && hasChildren);
      rows.push({ node, depth, hasChildren, isExpanded, isDisabled: disabled.has(node.id), isExcluded });
      if (isExpanded) {
        node.children.forEach((child) => walk(child, depth + 1));
      }
    };

    this.nodes().forEach((root) => walk(root, 0));
    return rows;
  });

  focusSearch(): void {
    this.searchInput()?.nativeElement.focus();
  }

  protected isPicked(doorId: number): boolean {
    return this.pickedSet().has(doorId);
  }

  protected rowAriaLabel(row: AbwabDoorPickerRow): string {
    const tag = this.disabledTag();
    return row.isDisabled && tag !== '' ? `${row.node.name} — ${tag}` : row.node.name;
  }

  protected excludedRowAriaLabel(row: AbwabDoorPickerRow): string | null {
    const tag = this.excludedTag();
    if (!row.isExcluded) {
      return null;
    }
    return tag === '' ? row.node.name : `${row.node.name} — ${tag}`;
  }

  protected expandAriaLabel(row: AbwabDoorPickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(row.node.name);
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected toggleExpanded(event: Event, row: AbwabDoorPickerRow): void {
    event.stopPropagation();
    if (row.isExcluded) {
      const next = new Set(this.collapsedExcludedIds());
      if (!next.delete(row.node.id)) {
        next.add(row.node.id);
      }
      this.collapsedExcludedIds.set(next);
      return;
    }
    const next = new Set(this.expandedIds());
    if (!next.delete(row.node.id)) {
      next.add(row.node.id);
    }
    this.expandedIds.set(next);
  }

  protected togglePicked(row: AbwabDoorPickerRow): void {
    if (row.isDisabled || row.isExcluded) {
      return;
    }
    this.toggled.emit(row.node.id);
  }

  reset(): void {
    this.searchQuery.set('');
    this.expandedIds.set(new Set());
    this.collapsedExcludedIds.set(new Set());
  }
}
