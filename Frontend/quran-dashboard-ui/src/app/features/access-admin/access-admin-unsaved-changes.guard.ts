import type { CanDeactivateFn } from '@angular/router';

import { ACCESS_ADMIN_LABELS } from './models/access-admin.labels';
import type { AccessAdminPageComponent } from './pages/access-admin-page/access-admin-page.component';

export const accessAdminUnsavedChangesGuard: CanDeactivateFn<AccessAdminPageComponent> = (page) =>
  !page.hasUnsavedChanges() || window.confirm(ACCESS_ADMIN_LABELS.unsavedChangesLeavePrompt);
