import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { LogtoSubjectRelinkPreview } from '../../../../core/api/generated/models/logto-subject-relink-preview';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessRelinkPreviewRequest,
  OWNER_RELINK_REQUIRED_CANDIDATE_STATE,
} from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-advanced-security',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective, QdEmptyStateComponent, QdFormFieldComponent],
  templateUrl: './access-advanced-security.component.html',
  styleUrl: './access-advanced-security.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessAdvancedSecurityComponent {
  readonly user = input.required<AccessUserDetail | null>();
  readonly relinkPreview = input<LogtoSubjectRelinkPreview | null>(null);
  readonly busyAction = input<string | null>(null);
  readonly hasUnsavedPermissions = input(false);
  readonly resetToken = input(0);

  readonly relinkPreviewRequested = output<AccessRelinkPreviewRequest>();
  readonly relinkConfirmed = output<string>();
  readonly relinkCancelled = output<void>();

  protected readonly newSub = signal('');
  protected readonly evidenceToken = signal('');
  protected readonly relinkReason = signal('');
  protected readonly relinkConfirmation = signal(false);

  constructor() {
    effect(() => {
      this.user()?.id;
      this.resetToken();
      this.resetRelinkForm();
    });
  }

  protected get requiredCandidateStateLabel(): string {
    return ACCESS_ADMIN_LABELS.reconciliationCandidateState(OWNER_RELINK_REQUIRED_CANDIDATE_STATE);
  }

  protected updateNewSub(event: Event): void {
    this.newSub.set((event.target as HTMLInputElement).value);
  }

  protected updateEvidenceToken(event: Event): void {
    this.evidenceToken.set((event.target as HTMLInputElement).value);
  }

  protected requestRelinkPreview(): void {
    const newSub = this.newSub().trim();
    const evidenceToken = this.evidenceToken().trim();
    if (!newSub || !evidenceToken || this.busyAction() || this.hasUnsavedPermissions()) {
      return;
    }
    this.relinkPreviewRequested.emit({ newSub, evidenceToken });
    this.evidenceToken.set('');
  }

  protected updateRelinkReason(event: Event): void {
    this.relinkReason.set((event.target as HTMLTextAreaElement).value);
  }

  protected updateRelinkConfirmation(event: Event): void {
    this.relinkConfirmation.set((event.target as HTMLInputElement).checked);
  }

  protected confirmRelink(): void {
    if (
      !this.relinkPreview()
      || !this.relinkReason().trim()
      || !this.relinkConfirmation()
      || this.busyAction()
      || this.hasUnsavedPermissions()
    ) {
      return;
    }
    this.relinkConfirmed.emit(this.relinkReason().trim());
  }

  protected cancelRelink(): void {
    if (this.busyAction()) {
      return;
    }
    this.resetRelinkForm();
    this.relinkCancelled.emit();
  }

  private resetRelinkForm(): void {
    this.newSub.set('');
    this.evidenceToken.set('');
    this.relinkReason.set('');
    this.relinkConfirmation.set(false);
  }
}
