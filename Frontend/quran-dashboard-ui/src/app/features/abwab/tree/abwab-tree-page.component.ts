import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ApplyFullProtectionPresetRequest, CategoryProtectionProfileDto, ManualProtectionType } from '../../../core/api/generated/models';
import { QdStateComponent } from '../../../shared/ui/state/state.component';
import { AbwabConflictError, ABWAB_CONFLICT_MESSAGES } from '../data-access/abwab-conflict';
import {
  CategoryEditorComponent,
  CategoryEditorSaveEvent,
  ExistingAliasEditEvent,
  ExistingAliasRemoveEvent,
} from '../editor/category-editor.component';
import {
  ApplyFullPresetEvent,
  ApplyProtectionEvent,
  LiftProtectionEvent,
  ProtectionPanelComponent,
} from '../protection/protection-panel.component';
import { AbwabPermissions } from '../state/abwab-permissions';
import { AbwabTreeFacade } from '../state/abwab-tree.facade';
import { buildVisibleTreeNodes } from './abwab-tree-node';
import { AbwabTreeViewComponent } from './abwab-tree-view.component';

// Wire-format keys for ApplyFullProtectionPresetRequest.expectedVersions, indexed by the numeric
// ManualProtectionType (0-4) — see manual-protection-type.ts / protection-type-labels.ts.
const PRESET_TYPE_KEYS: readonly (keyof ApplyFullProtectionPresetRequest['expectedVersions'])[] = [
  'CategoryData',
  'InternalStructure',
  'QuranContent',
  'Deletion',
  'Relationship',
];

const NOT_AUTHORIZED_MESSAGE = 'لا تملك صلاحية عرض شجرة الأبواب.';
const LOAD_ERROR_MESSAGE = 'تعذّر تحميل شجرة الأبواب.';

@Component({
  selector: 'qd-abwab-tree-page',
  standalone: true,
  imports: [ReactiveFormsModule, QdStateComponent, AbwabTreeViewComponent, CategoryEditorComponent, ProtectionPanelComponent],
  providers: [AbwabTreeFacade, AbwabPermissions],
  templateUrl: './abwab-tree-page.component.html',
  styleUrl: './abwab-tree-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTreePageComponent implements OnInit {
  protected readonly facade = inject(AbwabTreeFacade);
  protected readonly permissions = inject(AbwabPermissions);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly notAuthorizedMessage = NOT_AUTHORIZED_MESSAGE;
  protected readonly loadErrorMessage = LOAD_ERROR_MESSAGE;

  protected readonly visibleNodes = computed(() => buildVisibleTreeNodes(this.facade.categories(), this.facade.expandedCategoryIds()));
  protected readonly moveTargetCategoryId = signal<string | null>(null);
  protected readonly protectionProfile = signal<CategoryProtectionProfileDto | null>(null);
  protected readonly conflictMessage = computed(() => {
    const error = this.facade.mutationError();
    return error instanceof AbwabConflictError ? ABWAB_CONFLICT_MESSAGES[error.code] : null;
  });

  protected readonly newRootCategoryForm = this.formBuilder.group({
    name: this.formBuilder.control('', [Validators.required, Validators.maxLength(256)]),
  });

  constructor() {
    effect(() => {
      const category = this.facade.selectedCategory();
      if (!category || !this.permissions.canViewProtectionDetail()) {
        this.protectionProfile.set(null);
        return;
      }
      void this.facade.loadProtectionProfile(category.categoryId).then((profile) => this.protectionProfile.set(profile));
    });
  }

  ngOnInit(): void {
    void this.facade.load();
  }

  protected retry(): void {
    void this.facade.load();
  }

  protected selectCategory(categoryId: string): void {
    this.facade.selectCategory(categoryId);
  }

  protected toggleExpand(categoryId: string): void {
    this.facade.toggleExpanded(categoryId);
  }

  protected onSearch(event: Event): void {
    void this.facade.search((event.target as HTMLInputElement).value);
  }

  protected submitNewRootCategory(): void {
    if (this.newRootCategoryForm.invalid) {
      this.newRootCategoryForm.markAllAsTouched();
      return;
    }
    const snapshot = this.facade.snapshot();
    if (!snapshot) {
      return;
    }
    void this.facade
      .addCategory({
        name: this.newRootCategoryForm.getRawValue().name.trim(),
        description: null,
        representativeQuranExcerpt: null,
        parentCategoryId: null,
        sectionId: null,
        expectedTreeRevision: snapshot.treeRevision,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .then(() => this.newRootCategoryForm.reset({ name: '' }))
      .catch(() => undefined);
  }

  protected saveCategory(event: CategoryEditorSaveEvent): void {
    const category = this.facade.selectedCategory();
    const snapshot = this.facade.snapshot();
    if (!category || !snapshot) {
      return;
    }
    void this.facade
      .editCategory(category.categoryId, {
        name: event.name,
        description: event.description,
        representativeQuranExcerpt: event.representativeQuranExcerpt,
        expectedVersion: category.version,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .catch(() => undefined);

    for (const aliasValue of event.newAliases) {
      void this.facade
        .addCategoryAlias(category.categoryId, { value: aliasValue, expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation })
        .catch(() => undefined);
    }
  }

  protected editExistingAlias(event: ExistingAliasEditEvent): void {
    const snapshot = this.facade.snapshot();
    if (!snapshot) {
      return;
    }
    void this.facade
      .editCategoryAlias(event.aliasId, {
        value: event.value,
        expectedVersion: event.expectedVersion,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .catch(() => undefined);
  }

  protected removeExistingAlias(event: ExistingAliasRemoveEvent): void {
    const snapshot = this.facade.snapshot();
    if (!snapshot) {
      return;
    }
    void this.facade
      .removeCategoryAlias(event.aliasId, {
        expectedVersion: event.expectedVersion,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .catch(() => undefined);
  }

  protected moveUp(categoryId: string): void {
    this.reorderSibling(categoryId, -1);
  }

  protected moveDown(categoryId: string): void {
    this.reorderSibling(categoryId, 1);
  }

  private reorderSibling(categoryId: string, direction: -1 | 1): void {
    const snapshot = this.facade.snapshot();
    const category = snapshot?.categories.find((entry) => entry.categoryId === categoryId);
    if (!snapshot || !category) {
      return;
    }
    const currentOrder = category.parentCategoryId === null ? category.sectionOrder ?? 0 : category.siblingOrder ?? 0;
    void this.facade
      .reorderCategories({
        scope: category.parentCategoryId === null ? 1 : 0,
        parentCategoryId: category.parentCategoryId,
        sectionId: category.parentCategoryId === null ? category.sectionId : null,
        orders: [{ categoryId, newOrder: currentOrder + direction, expectedVersion: category.version }],
        expectedTreeRevision: snapshot.treeRevision,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .catch(() => undefined);
  }

  protected requestMove(categoryId: string): void {
    this.moveTargetCategoryId.set(categoryId);
  }

  protected cancelMove(): void {
    this.moveTargetCategoryId.set(null);
  }

  protected confirmMove(destinationCategoryId: string | null): void {
    const categoryId = this.moveTargetCategoryId();
    const snapshot = this.facade.snapshot();
    const category = snapshot?.categories.find((entry) => entry.categoryId === categoryId);
    if (!categoryId || !snapshot || !category) {
      return;
    }
    void this.facade
      .moveCategories({
        moves: [{ categoryId, newParentCategoryId: destinationCategoryId, newSectionId: null, expectedVersion: category.version }],
        expectedTreeRevision: snapshot.treeRevision,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .then(() => this.moveTargetCategoryId.set(null))
      .catch(() => undefined);
  }

  protected requestDelete(categoryId: string): void {
    const snapshot = this.facade.snapshot();
    const category = snapshot?.categories.find((entry) => entry.categoryId === categoryId);
    if (!snapshot || !category) {
      return;
    }
    void this.facade
      .subtreeDeleteCategory(categoryId, {
        expectedVersion: category.version,
        expectedTreeRevision: snapshot.treeRevision,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .catch(() => undefined);
  }

  protected applyProtection(event: ApplyProtectionEvent): void {
    const category = this.facade.selectedCategory();
    const snapshot = this.facade.snapshot();
    if (!category || !snapshot) {
      return;
    }
    void this.facade
      .applyManualProtection(category.categoryId, event.protectionType, {
        scope: event.scope,
        expectedVersion: null,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .then(() => this.facade.loadProtectionProfile(category.categoryId))
      .then((profile) => this.protectionProfile.set(profile))
      .catch(() => undefined);
  }

  protected liftProtection(event: LiftProtectionEvent): void {
    const category = this.facade.selectedCategory();
    const snapshot = this.facade.snapshot();
    const resolution = this.directResolutionFor(event.protectionType);
    if (!category || !snapshot || resolution?.version == null) {
      return;
    }
    void this.facade
      .liftManualProtection(category.categoryId, event.protectionType, {
        expectedVersion: resolution.version,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .then(() => this.facade.loadProtectionProfile(category.categoryId))
      .then((profile) => this.protectionProfile.set(profile))
      .catch(() => undefined);
  }

  protected applyFullPreset(event: ApplyFullPresetEvent): void {
    const category = this.facade.selectedCategory();
    const snapshot = this.facade.snapshot();
    if (!category || !snapshot) {
      return;
    }

    // Each changed-scope type needs its OWN active record's Version; a type with no active record
    // (being newly inserted by the preset) is never version-checked by the write path, so 0 there is
    // an inert placeholder, not a manufactured expectation (manual-protection-contract.md).
    const expectedVersions = PRESET_TYPE_KEYS.reduce(
      (versions, key, index) => {
        versions[key] = this.directResolutionFor(index as ManualProtectionType)?.version ?? 0;
        return versions;
      },
      {} as ApplyFullProtectionPresetRequest['expectedVersions'],
    );

    void this.facade
      .applyFullProtectionPreset(category.categoryId, {
        scope: event.scope,
        expectedVersions,
        expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
      })
      .then(() => this.facade.loadProtectionProfile(category.categoryId))
      .then((profile) => this.protectionProfile.set(profile))
      .catch(() => undefined);
  }

  private directResolutionFor(protectionType: ManualProtectionType) {
    return this.protectionProfile()?.manualProtections.find(
      (resolution) => resolution.protectionType === protectionType && resolution.isDirect,
    );
  }
}
