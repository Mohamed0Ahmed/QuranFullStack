import { InjectionToken } from '@angular/core';

export function injectionToken<T>(description: string, factory?: () => T): InjectionToken<T> {
  return factory
    ? new InjectionToken<T>(description, { providedIn: 'root', factory })
    : new InjectionToken<T>(description);
}
