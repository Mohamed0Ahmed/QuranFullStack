import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { CategoryRelationshipDto, RelationshipType } from '../../../core/api/generated/models';
import { QdStateComponent } from '../../../shared/ui/state/state.component';
import { ABWAB_CONFLICT_MESSAGES, AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_RELATIONSHIPS_CACHE_PROVIDER } from '../data-access/abwab-relationships-cache';
import { RELATIONSHIP_TYPE_SIMILAR } from '../data-access/abwab-relationships.port';
import { AbwabPermissions } from '../state/abwab-permissions';
import { AbwabRelationshipsFacade } from '../state/abwab-relationships.facade';
import {
  DIRECTIONAL_RELATIONSHIP_TYPE,
  RELATIONSHIP_TYPE_LABELS,
  RELATIONSHIP_TYPE_OPTIONS,
  relationshipEndpointLabels,
} from '../data-access/relationship-type-labels';

const NOT_AUTHORIZED_MESSAGE = 'لا تملك صلاحية عرض علاقات الأبواب.';
const EMPTY_MESSAGE = 'لا توجد علاقات مسجّلة لهذا الباب.';
const LOADING_MESSAGE = 'جارٍ تحميل العلاقات…';
const RETRY_LABEL = 'إعادة المحاولة';
const INCLUDE_DELETED_PARAM = 'includeDeleted';

export type RelationshipDirection = 'routed-is-broader' | 'routed-is-narrower';

const DIRECTION_LABELS: Record<RelationshipDirection, string> = {
  'routed-is-broader': 'هذا الباب هو الأعم',
  'routed-is-narrower': 'هذا الباب هو الأخص',
};

const DIRECTION_OPTIONS: readonly RelationshipDirection[] = ['routed-is-broader', 'routed-is-narrower'];

type RelationshipForm = FormGroup<{
  relationshipType: FormControl<RelationshipType>;
  otherCategoryId: FormControl<string>;
  direction: FormControl<RelationshipDirection>;
}>;

@Component({
  selector: 'qd-abwab-relationships-page',
  standalone: true,
  imports: [ReactiveFormsModule, QdStateComponent],
  providers: [AbwabRelationshipsFacade, ABWAB_RELATIONSHIPS_CACHE_PROVIDER, AbwabPermissions],
  templateUrl: './abwab-relationships-page.component.html',
  styleUrl: './abwab-relationships-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabRelationshipsPageComponent implements OnInit {
  protected readonly facade = inject(AbwabRelationshipsFacade);
  protected readonly permissions = inject(AbwabPermissions);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly notAuthorizedMessage = NOT_AUTHORIZED_MESSAGE;
  protected readonly emptyMessage = EMPTY_MESSAGE;
  protected readonly loadingMessage = LOADING_MESSAGE;
  protected readonly retryLabel = RETRY_LABEL;
  // Getters, because a class-FIELD initialiser must never read an IMPORTED binding here: under the
  // unit-test builder esbuild emits shared modules as lazily-initialised chunks, and vite-node's SSR
  // transform hoists an import referenced in class-field position into a module-top `const` snapshot
  // that evaluates BEFORE that chunk's body — capturing `undefined`, which makes `@for` render a
  // <select> with zero options and no error. Reads from a method/getter body, and from the
  // @Component metadata, stay live property accesses and are unaffected.
  protected get typeLabels(): Record<RelationshipType, string> {
    return RELATIONSHIP_TYPE_LABELS;
  }

  protected get typeOptions(): readonly RelationshipType[] {
    return RELATIONSHIP_TYPE_OPTIONS;
  }

  protected readonly directionLabels = DIRECTION_LABELS;
  protected readonly directionOptions = DIRECTION_OPTIONS;

  protected readonly editingRelationshipId = signal<string | null>(null);

  protected readonly conflictMessage = computed(() => {
    const error = this.facade.mutationError();
    return error instanceof AbwabConflictError ? ABWAB_CONFLICT_MESSAGES[error.code] : null;
  });

  protected readonly mutationErrorMessage = computed(() => {
    const error = this.facade.mutationError();
    return error === null || error instanceof AbwabConflictError ? null : error.message;
  });

  // Add and edit own SEPARATE groups: sharing one would let opening the editor discard an unsaved
  // add draft, and cancelling the editor would leave the edited row's values in the add panel.
  protected readonly addForm = this.buildRelationshipForm();
  protected readonly editForm = this.buildRelationshipForm();

  protected readonly addIsDirectional = signal(false);
  protected readonly editIsDirectional = signal(false);

  ngOnInit(): void {
    const categoryId = this.route.snapshot.paramMap.get('categoryId');
    if (!categoryId) {
      return;
    }

    this.trackDirectionalSelection(this.addForm, this.addIsDirectional);
    this.trackDirectionalSelection(this.editForm, this.editIsDirectional);

    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => void this.facade.load(categoryId, params.get(INCLUDE_DELETED_PARAM) === 'true'));
  }

  protected toggleIncludeDeleted(includeDeleted: boolean): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [INCLUDE_DELETED_PARAM]: includeDeleted ? 'true' : null },
      queryParamsHandling: 'merge',
    });
  }

  protected submitAdd(): void {
    const categoryId = this.facade.categoryId();
    const generation = this.facade.expectedTimelineGeneration();
    if (!categoryId || generation === null || this.addForm.invalid) {
      return;
    }

    const { relationshipType } = this.addForm.getRawValue();
    void this.facade
      .addRelationship({
        relationshipType,
        ...this.orientedEndpoints(this.addForm, categoryId),
        expectedTimelineGeneration: generation,
      })
      // Unsaved input is preserved on conflict so the operator never retypes it (§14.3).
      .then(() => this.addForm.reset())
      .catch(() => undefined);
  }

  protected beginEdit(relationship: CategoryRelationshipDto): void {
    this.editingRelationshipId.set(relationship.categoryRelationshipId);
    this.editIsDirectional.set(relationship.isDirectional);
    this.editForm.setValue({
      relationshipType: relationship.relationshipType,
      otherCategoryId: this.otherEndpointOf(relationship),
      direction: this.storedDirectionOf(relationship),
    });
  }

  protected cancelEdit(): void {
    this.editingRelationshipId.set(null);
  }

  protected submitEdit(relationship: CategoryRelationshipDto): void {
    const categoryId = this.facade.categoryId();
    const generation = this.facade.expectedTimelineGeneration();
    if (!categoryId || generation === null || this.editForm.invalid) {
      return;
    }

    const { relationshipType } = this.editForm.getRawValue();
    void this.facade
      .editRelationship(relationship.categoryRelationshipId, {
        relationshipType,
        ...this.orientedEndpoints(this.editForm, categoryId),
        expectedVersion: relationship.version,
        expectedTimelineGeneration: generation,
      })
      .then(() => this.editingRelationshipId.set(null))
      .catch(() => undefined);
  }

  protected remove(relationship: CategoryRelationshipDto): void {
    const generation = this.facade.expectedTimelineGeneration();
    if (generation === null) {
      return;
    }

    void this.facade
      .deleteRelationship(relationship.categoryRelationshipId, {
        expectedVersion: relationship.version,
        expectedTimelineGeneration: generation,
      })
      .catch(() => undefined);
  }

  protected restore(relationship: CategoryRelationshipDto): void {
    const generation = this.facade.expectedTimelineGeneration();
    if (generation === null) {
      return;
    }

    void this.facade
      .restoreRelationship(relationship.categoryRelationshipId, {
        expectedVersion: relationship.version,
        expectedTimelineGeneration: generation,
      })
      .catch(() => undefined);
  }

  protected reload(): void {
    void this.facade.reload();
  }

  protected endpointLabels(isDirectional: boolean): { from: string; to: string } {
    return relationshipEndpointLabels(isDirectional);
  }

  // A directional row is stored broader-first (Source=first, Target=second) and read back in that
  // same order, so the submitted pair must follow the chosen direction — pinning the routed door as
  // `first` would silently invert an inbound edge on a no-op save.
  private orientedEndpoints(form: RelationshipForm, routedCategoryId: string): { firstCategoryId: string; secondCategoryId: string } {
    const { relationshipType, otherCategoryId, direction } = form.getRawValue();
    const routedIsBroader = relationshipType !== DIRECTIONAL_RELATIONSHIP_TYPE || direction === 'routed-is-broader';

    return routedIsBroader
      ? { firstCategoryId: routedCategoryId, secondCategoryId: otherCategoryId }
      : { firstCategoryId: otherCategoryId, secondCategoryId: routedCategoryId };
  }

  private storedDirectionOf(relationship: CategoryRelationshipDto): RelationshipDirection {
    if (!relationship.isDirectional) {
      return 'routed-is-broader';
    }
    return relationship.to.categoryId === this.facade.categoryId() ? 'routed-is-narrower' : 'routed-is-broader';
  }

  private otherEndpointOf(relationship: CategoryRelationshipDto): string {
    const categoryId = this.facade.categoryId();
    return relationship.from.categoryId === categoryId ? relationship.to.categoryId : relationship.from.categoryId;
  }

  private rejectSelfLink(group: AbstractControl): ValidationErrors | null {
    const otherCategoryId = group.get('otherCategoryId')?.value as string | undefined;
    return otherCategoryId && otherCategoryId === this.facade.categoryId() ? { selfLink: true } : null;
  }

  private buildRelationshipForm(): RelationshipForm {
    return this.formBuilder.group(
      {
        relationshipType: this.formBuilder.control<RelationshipType>(RELATIONSHIP_TYPE_SIMILAR, [Validators.required]),
        otherCategoryId: this.formBuilder.control('', [Validators.required]),
        direction: this.formBuilder.control<RelationshipDirection>('routed-is-broader', [Validators.required]),
      },
      { validators: (group: AbstractControl): ValidationErrors | null => this.rejectSelfLink(group) },
    );
  }

  private trackDirectionalSelection(form: RelationshipForm, isDirectional: WritableSignal<boolean>): void {
    form.controls.relationshipType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => isDirectional.set(type === DIRECTIONAL_RELATIONSHIP_TYPE));
  }
}
