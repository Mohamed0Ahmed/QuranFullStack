import { ChangeDetectionStrategy, Component, OnChanges, inject, input, output } from '@angular/core';
import { FormArray, FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CategorySearchAliasDto, CategorySnapshotDto } from '../../../core/api/generated/models';

export interface CategoryEditorSaveEvent {
  readonly name: string;
  readonly description: string | null;
  readonly representativeQuranExcerpt: string | null;
  readonly newAliases: readonly string[];
}

export interface ExistingAliasEditEvent {
  readonly aliasId: string;
  readonly value: string;
  readonly expectedVersion: number;
}

export interface ExistingAliasRemoveEvent {
  readonly aliasId: string;
  readonly expectedVersion: number;
}

// Existing-alias edit/remove each carry the ALIAS'S OWN Version (from CategorySnapshotDto.aliases)
// as expectedVersion — never the category's.
@Component({
  selector: 'qd-abwab-category-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './category-editor.component.html',
  styleUrl: './category-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryEditorComponent implements OnChanges {
  readonly category = input<CategorySnapshotDto | null>(null);
  readonly canEdit = input(false);
  readonly saving = input(false);

  readonly save = output<CategoryEditorSaveEvent>();
  readonly editAlias = output<ExistingAliasEditEvent>();
  readonly removeAlias = output<ExistingAliasRemoveEvent>();

  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly existingAliasControls = new Map<string, FormControl<string>>();

  readonly form = this.formBuilder.group({
    name: this.formBuilder.control('', [Validators.required, Validators.maxLength(256)]),
    description: this.formBuilder.control(''),
    representativeQuranExcerpt: this.formBuilder.control(''),
    newAliases: this.formBuilder.array<string>([]),
  });

  ngOnChanges(): void {
    const category = this.category();
    this.form.patchValue({
      name: category?.name ?? '',
      description: category?.description ?? '',
      representativeQuranExcerpt: category?.representativeQuranExcerpt ?? '',
    });
    this.newAliasesArray.clear();

    this.existingAliasControls.clear();
    for (const alias of category?.aliases ?? []) {
      this.existingAliasControls.set(alias.categorySearchAliasId, this.formBuilder.control(alias.value, Validators.required));
    }
  }

  get newAliasesArray(): FormArray<FormControl<string>> {
    return this.form.controls.newAliases;
  }

  controlForAlias(aliasId: string): FormControl<string> {
    return this.existingAliasControls.get(aliasId)!;
  }

  addAliasField(): void {
    this.newAliasesArray.push(this.formBuilder.control('', Validators.required));
  }

  removeAliasField(index: number): void {
    this.newAliasesArray.removeAt(index);
  }

  saveExistingAlias(alias: CategorySearchAliasDto): void {
    const control = this.controlForAlias(alias.categorySearchAliasId);
    if (control.invalid) {
      control.markAsTouched();
      return;
    }
    const value = control.value.trim();
    if (value.length === 0) {
      control.markAsTouched();
      return;
    }
    this.editAlias.emit({ aliasId: alias.categorySearchAliasId, value, expectedVersion: alias.version });
  }

  deleteExistingAlias(alias: CategorySearchAliasDto): void {
    this.removeAlias.emit({ aliasId: alias.categorySearchAliasId, expectedVersion: alias.version });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      name: value.name.trim(),
      description: value.description.trim().length > 0 ? value.description.trim() : null,
      representativeQuranExcerpt: value.representativeQuranExcerpt.trim().length > 0 ? value.representativeQuranExcerpt.trim() : null,
      newAliases: value.newAliases.map((alias: string) => alias.trim()).filter((alias: string) => alias.length > 0),
    });
  }
}
