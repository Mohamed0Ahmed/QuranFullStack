import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PermissionCode } from '../../../../core/auth/permission-code';
import {
  AccessPermissionGroup,
  permissionLabelFor,
  setGroupSelection,
  setIndividualSelection,
} from '../../models/access-admin-permissions';

@Component({
  selector: 'qd-access-permission-editor',
  standalone: true,
  templateUrl: './access-permission-editor.component.html',
  styleUrl: './access-permission-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessPermissionEditorComponent {
  readonly groups = input.required<readonly AccessPermissionGroup[]>();
  readonly selectedCodes = input.required<ReadonlySet<PermissionCode>>();
  readonly disabled = input(false);

  readonly selectionChange = output<PermissionCode[]>();

  protected isGroupSelected(group: AccessPermissionGroup): boolean {
    return group.codes.every((code) => this.selectedCodes().has(code));
  }

  protected isGroupPartiallySelected(group: AccessPermissionGroup): boolean {
    return !this.isGroupSelected(group) && group.codes.some((code) => this.selectedCodes().has(code));
  }

  protected isCodeSelected(code: PermissionCode): boolean {
    return this.selectedCodes().has(code);
  }

  protected labelFor(group: AccessPermissionGroup, code: PermissionCode): string {
    return permissionLabelFor([group], code);
  }

  protected changeGroup(group: AccessPermissionGroup, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.emitSelection(setGroupSelection(this.selectedCodes(), group, checked));
  }

  protected changeCode(code: PermissionCode, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.emitSelection(setIndividualSelection(this.selectedCodes(), code, checked));
  }

  private emitSelection(selected: ReadonlySet<PermissionCode>): void {
    const allowed = new Set(this.groups().flatMap((group) => group.codes));
    this.selectionChange.emit([...allowed].filter((code) => selected.has(code)));
  }
}
