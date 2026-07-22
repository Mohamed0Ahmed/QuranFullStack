import { InjectionToken } from '@angular/core';

// Stable DI convention: every app-wide token is declared through this helper so the description prefix and
// root-provided factory shape stay consistent across the app. A token with a factory is tree-shakable and
// self-providing; a token without one is resolved from an explicit provider at the injection site.
export function injectionToken<T>(description: string, factory?: () => T): InjectionToken<T> {
  return factory
    ? new InjectionToken<T>(description, { providedIn: 'root', factory })
    : new InjectionToken<T>(description);
}
