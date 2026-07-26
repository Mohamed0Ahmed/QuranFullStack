import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { TemplateNodeAliasDto, TemplateNodeDto } from '../../../core/api/generated/models';
import { AbwabTemplatesMutationStatus } from '../state/abwab-templates.facade';

export interface TemplateNodeAddRequest {
  readonly parentTemplateNodeId: string | null;
  readonly name: string;
  readonly representativeQuranExcerpt: string | null;
  readonly description: string | null;
}

export interface TemplateNodeEditRequest {
  readonly node: TemplateNodeDto;
  readonly name: string;
  readonly representativeQuranExcerpt: string | null;
  readonly description: string | null;
}

export interface TemplateNodeReparentRequest {
  readonly node: TemplateNodeDto;
  readonly newParentTemplateNodeId: string | null;
}

export interface TemplateNodeReorderRequest {
  readonly parentTemplateNodeId: string | null;
  readonly orderedTemplateNodeIds: readonly string[];
}

export interface TemplateNodeAliasAddRequest {
  readonly node: TemplateNodeDto;
  readonly value: string;
}

export interface TemplateNodeAliasEditRequest {
  readonly alias: TemplateNodeAliasDto;
  readonly value: string;
}

export interface TemplateNodeRow {
  readonly node: TemplateNodeDto;
  readonly depth: number;
  readonly canMoveUp: boolean;
  readonly canMoveDown: boolean;
}

type NodeForm = FormGroup<{
  name: FormControl<string>;
  representativeQuranExcerpt: FormControl<string>;
  description: FormControl<string>;
  parentTemplateNodeId: FormControl<string>;
}>;

type AliasForm = FormGroup<{
  value: FormControl<string>;
}>;

// The node half of the editor: forms, tree flattening, and the explicit row actions. It owns no
// facade and no concurrency expectations — it emits intent, the page turns that into a mutation.
// Ordering and reparent are EXPLICIT actions: move-up/move-down emit the recomputed sibling order and
// a reparent names its destination, so there is no drop target to infer anything from.
// `mutationStatus` MUST be an EDITOR-scoped status: typed input is discarded only once the server has
// accepted the write, so a conflict leaves the operator's form exactly as they left it (§14.3).
@Component({
  selector: 'qd-abwab-template-node-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './template-node-editor.component.html',
  styleUrl: './template-node-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TemplateNodeEditorComponent {
  readonly nodes = input.required<readonly TemplateNodeDto[]>();
  readonly canEdit = input(false);
  readonly mutationStatus = input<AbwabTemplatesMutationStatus>('idle');

  readonly addNode = output<TemplateNodeAddRequest>();
  readonly editNode = output<TemplateNodeEditRequest>();
  readonly reparentNode = output<TemplateNodeReparentRequest>();
  readonly reorderNodes = output<TemplateNodeReorderRequest>();
  readonly removeNode = output<TemplateNodeDto>();
  readonly addAlias = output<TemplateNodeAliasAddRequest>();
  readonly editAlias = output<TemplateNodeAliasEditRequest>();
  readonly removeAlias = output<TemplateNodeAliasDto>();
  readonly restoreAlias = output<TemplateNodeAliasDto>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly editingNodeId = signal<string | null>(null);
  protected readonly aliasAddingNodeId = signal<string | null>(null);
  protected readonly editingAliasId = signal<string | null>(null);

  protected readonly addForm: NodeForm = this.buildNodeForm();
  protected readonly editForm: NodeForm = this.buildNodeForm();
  protected readonly aliasAddForm: AliasForm = this.buildAliasForm();
  protected readonly aliasEditForm: AliasForm = this.buildAliasForm();

  protected readonly activeNodes = computed(() => this.nodes().filter((node) => !node.isDeleted));

  protected readonly rows = computed<readonly TemplateNodeRow[]>(() => {
    const nodes = this.activeNodes();
    const rows: TemplateNodeRow[] = [];

    const walk = (parentTemplateNodeId: string | null, depth: number): void => {
      const siblings = this.siblingsOf(nodes, parentTemplateNodeId);
      siblings.forEach((node, index) => {
        rows.push({ node, depth, canMoveUp: index > 0, canMoveDown: index < siblings.length - 1 });
        walk(node.templateNodeId, depth + 1);
      });
    };

    walk(null, 0);
    return rows;
  });

  protected readonly parentOptions = computed(() =>
    this.activeNodes().filter((node) => node.templateNodeId !== this.editingNodeId()),
  );

  constructor() {
    effect(() => {
      if (this.mutationStatus() === 'success') {
        this.discardAcceptedInput();
      }
    });
  }

  protected submitAdd(): void {
    if (this.addForm.invalid) {
      return;
    }

    const { name, representativeQuranExcerpt, description, parentTemplateNodeId } = this.addForm.getRawValue();
    this.addNode.emit({
      parentTemplateNodeId: parentTemplateNodeId || null,
      name,
      representativeQuranExcerpt: representativeQuranExcerpt || null,
      description: description || null,
    });
  }

  protected beginEdit(node: TemplateNodeDto): void {
    this.editingNodeId.set(node.templateNodeId);
    this.editForm.setValue({
      name: node.name,
      representativeQuranExcerpt: node.representativeQuranExcerpt ?? '',
      description: node.description ?? '',
      parentTemplateNodeId: node.parentTemplateNodeId ?? '',
    });
  }

  protected cancelEdit(): void {
    this.editingNodeId.set(null);
  }

  protected submitEdit(node: TemplateNodeDto): void {
    if (this.editForm.invalid) {
      return;
    }

    const { name, representativeQuranExcerpt, description } = this.editForm.getRawValue();
    this.editNode.emit({
      node,
      name,
      representativeQuranExcerpt: representativeQuranExcerpt || null,
      description: description || null,
    });
  }

  protected submitReparent(node: TemplateNodeDto): void {
    const { parentTemplateNodeId } = this.editForm.getRawValue();
    this.reparentNode.emit({ node, newParentTemplateNodeId: parentTemplateNodeId || null });
  }

  protected move(row: TemplateNodeRow, offset: -1 | 1): void {
    const siblings = this.siblingsOf(this.activeNodes(), row.node.parentTemplateNodeId).map((node) => node.templateNodeId);
    const index = siblings.indexOf(row.node.templateNodeId);
    const target = index + offset;
    if (index < 0 || target < 0 || target >= siblings.length) {
      return;
    }

    [siblings[index], siblings[target]] = [siblings[target], siblings[index]];
    this.reorderNodes.emit({ parentTemplateNodeId: row.node.parentTemplateNodeId, orderedTemplateNodeIds: siblings });
  }

  protected remove(node: TemplateNodeDto): void {
    this.removeNode.emit(node);
  }

  protected beginAliasAdd(node: TemplateNodeDto): void {
    this.editingAliasId.set(null);
    this.aliasAddingNodeId.set(node.templateNodeId);
    this.aliasAddForm.reset();
  }

  protected cancelAliasAdd(): void {
    this.aliasAddingNodeId.set(null);
  }

  protected submitAliasAdd(node: TemplateNodeDto): void {
    if (this.aliasAddForm.invalid) {
      return;
    }

    this.addAlias.emit({ node, value: this.aliasAddForm.getRawValue().value });
  }

  protected beginAliasEdit(alias: TemplateNodeAliasDto): void {
    this.aliasAddingNodeId.set(null);
    this.editingAliasId.set(alias.templateNodeSearchAliasId);
    this.aliasEditForm.setValue({ value: alias.value });
  }

  protected cancelAliasEdit(): void {
    this.editingAliasId.set(null);
  }

  protected submitAliasEdit(alias: TemplateNodeAliasDto): void {
    if (this.aliasEditForm.invalid) {
      return;
    }

    this.editAlias.emit({ alias, value: this.aliasEditForm.getRawValue().value });
  }

  private discardAcceptedInput(): void {
    this.addForm.reset();
    this.editingNodeId.set(null);
    this.aliasAddForm.reset();
    this.aliasAddingNodeId.set(null);
    this.editingAliasId.set(null);
  }

  private siblingsOf(nodes: readonly TemplateNodeDto[], parentTemplateNodeId: string | null): TemplateNodeDto[] {
    return nodes
      .filter((node) => node.parentTemplateNodeId === parentTemplateNodeId)
      .sort((left, right) => left.siblingOrder - right.siblingOrder);
  }

  private buildNodeForm(): NodeForm {
    return this.formBuilder.group({
      name: this.formBuilder.control('', [Validators.required]),
      representativeQuranExcerpt: this.formBuilder.control(''),
      description: this.formBuilder.control(''),
      parentTemplateNodeId: this.formBuilder.control(''),
    });
  }

  private buildAliasForm(): AliasForm {
    return this.formBuilder.group({
      value: this.formBuilder.control('', [Validators.required]),
    });
  }
}
