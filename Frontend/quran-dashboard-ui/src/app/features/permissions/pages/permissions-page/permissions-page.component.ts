import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { PermissionTargetKind } from '../../models/permission.models';
import { PermissionsFacade } from '../../state/permissions.facade';

const MANAGE_PERMISSION = 'permission.administer';
const NOT_AUTHORIZED = 'لا تملك صلاحية إدارة الصلاحيات.';

// Owner-only permission administration (US5): the first real Reactive-Forms surface. The route is gated by
// permissionGuard, and this component ALSO hides the form when the caller lacks the manage permission — but
// that hiding is non-authoritative (the backend policy rejects a direct call regardless).
@Component({
  selector: 'qd-permissions-page',
  standalone: true,
  imports: [ReactiveFormsModule, QdStateComponent],
  providers: [PermissionsFacade],
  templateUrl: './permissions-page.component.html',
  styleUrl: './permissions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermissionsPageComponent implements OnInit {
  private readonly facade = inject(PermissionsFacade);
  private readonly currentUser = inject(CurrentUserStore);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly loadStatus = this.facade.loadStatus;
  readonly loadMessage = this.facade.loadMessage;
  readonly grantedAssignments = this.facade.grantedAssignments;
  readonly assignableCodes = this.facade.assignableCodes;
  readonly submitMessage = this.facade.submitMessage;
  readonly notAuthorizedMessage = NOT_AUTHORIZED;

  readonly canManage = computed(() => this.currentUser.hasPermission(MANAGE_PERMISSION));
  readonly isSubmitting = computed(
    () => this.facade.grantAction.isPending() || this.facade.revokeAction.isPending(),
  );

  readonly form = this.formBuilder.group({
    targetKind: this.formBuilder.control<PermissionTargetKind>('Role', Validators.required),
    targetKey: this.formBuilder.control('', [Validators.required, Validators.maxLength(256)]),
    permissionCode: this.formBuilder.control('', Validators.required),
  });

  ngOnInit(): void {
    void this.facade.load();
  }

  grant(): void {
    void this.mutate((request) => this.facade.grantAction.run(request));
  }

  revoke(): void {
    void this.mutate((request) => this.facade.revokeAction.run(request));
  }

  retry(): void {
    void this.facade.load();
  }

  private async mutate(run: (request: ReturnType<PermissionsFacade['buildRequest']>) => Promise<unknown>): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    await run(this.facade.buildRequest(value.targetKind, value.targetKey, value.permissionCode));
  }
}
