import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { CategoryProtectionProfileDto, ManualProtectionScope, ManualProtectionType } from '../../../core/api/generated/models';
import { MANUAL_PROTECTION_SCOPE_LABELS, MANUAL_PROTECTION_TYPE_LABELS } from './protection-type-labels';

export interface ApplyProtectionEvent {
  readonly protectionType: ManualProtectionType;
  readonly scope: ManualProtectionScope;
}

export interface LiftProtectionEvent {
  readonly protectionType: ManualProtectionType;
}

export interface ApplyFullPresetEvent {
  readonly scope: ManualProtectionScope;
}

const SCOPE_OPTIONS: readonly ManualProtectionScope[] = [0, 1];

// T072: protection UI — view direct/inherited protection with the resolving source ancestor and the
// server-derived expiry, and apply/lift/full-preset actions. Gated by protection.view: when canView
// is false this component renders NOTHING protection-specific, matching the backend's composite-read
// redaction (tree-read-contract.md) — the profile input is only ever populated by a caller that
// already holds protection.view, so there is no type/scope/actor/source-ancestor for this component
// to leak even if it were rendered.
@Component({
  selector: 'qd-abwab-protection-panel',
  standalone: true,
  templateUrl: './protection-panel.component.html',
  styleUrl: './protection-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProtectionPanelComponent {
  readonly profile = input<CategoryProtectionProfileDto | null>(null);
  readonly canView = input(false);
  readonly canApply = input(false);
  readonly canLift = input(false);

  readonly apply = output<ApplyProtectionEvent>();
  readonly lift = output<LiftProtectionEvent>();
  readonly applyFullPreset = output<ApplyFullPresetEvent>();

  protected readonly typeLabels = MANUAL_PROTECTION_TYPE_LABELS;
  protected readonly scopeLabels = MANUAL_PROTECTION_SCOPE_LABELS;
  protected readonly scopeOptions = SCOPE_OPTIONS;

  protected applyType(protectionType: ManualProtectionType, scope: ManualProtectionScope): void {
    this.apply.emit({ protectionType, scope });
  }

  protected liftType(protectionType: ManualProtectionType): void {
    this.lift.emit({ protectionType });
  }

  protected applyPreset(scope: ManualProtectionScope): void {
    this.applyFullPreset.emit({ scope });
  }

  protected scopeOf(select: EventTarget | null): ManualProtectionScope {
    const value = (select as HTMLSelectElement).value;
    return Number(value) as ManualProtectionScope;
  }
}
