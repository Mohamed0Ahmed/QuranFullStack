import type { CanDeactivateFn } from '@angular/router';

import type { AccessAdminPageComponent } from './pages/access-admin-page/access-admin-page.component';

export const accessAdminUnsavedChangesGuard: CanDeactivateFn<AccessAdminPageComponent> = (page) =>
  !page.hasUnsavedChanges() || page.confirmRouteLeave();
