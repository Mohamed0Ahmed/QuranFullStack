import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { accessAdminUnsavedChangesGuard } from './access-admin-unsaved-changes.guard';
import { ACCESS_ADMIN_LABELS } from './models/access-admin.labels';
import type { AccessAdminPageComponent } from './pages/access-admin-page/access-admin-page.component';

function deactivate(hasUnsavedChanges: boolean) {
  const page = { hasUnsavedChanges: () => hasUnsavedChanges } as AccessAdminPageComponent;
  return accessAdminUnsavedChangesGuard(
    page,
    {} as ActivatedRouteSnapshot,
    {} as RouterStateSnapshot,
    {} as RouterStateSnapshot,
  );
}

describe('accessAdminUnsavedChangesGuard', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('leaves without prompting when the permission draft matches the stored grants', () => {
    const confirmation = vi.spyOn(window, 'confirm').mockReturnValue(true);

    expect(deactivate(false)).toBe(true);
    expect(confirmation).not.toHaveBeenCalled();
  });

  it.each([
    ['discards the draft', true],
    ['keeps the operator on the page', false],
  ])('asks before leaving an unsaved draft and %s', (_outcome, answer) => {
    const confirmation = vi.spyOn(window, 'confirm').mockReturnValue(answer);

    expect(deactivate(true)).toBe(answer);
    expect(confirmation).toHaveBeenCalledWith(ACCESS_ADMIN_LABELS.unsavedChangesLeavePrompt);
  });
});
