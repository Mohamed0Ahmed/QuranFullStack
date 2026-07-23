import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { AbwabPermissions } from './abwab-permissions';

class StubCurrentUserStore {
  private permissions: string[] = [];
  setPermissions(permissions: string[]): void {
    this.permissions = permissions;
  }
  hasPermission(code: string): boolean {
    return this.permissions.includes(code);
  }
}

function createPermissions(granted: string[]): { permissions: AbwabPermissions; store: StubCurrentUserStore } {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [AbwabPermissions, { provide: CurrentUserStore, useClass: StubCurrentUserStore }],
  });
  const store = TestBed.inject(CurrentUserStore) as unknown as StubCurrentUserStore;
  store.setPermissions(granted);
  return { permissions: TestBed.inject(AbwabPermissions), store };
}

// T064: composite-read UI visibility mirrors the backend redaction table (tree-read-contract.md)
// across every grant combination — frontend hiding is COSMETIC, the redacted DTO itself (not this
// service) is what actually withholds type/scope/actor/source-ancestor data (see
// protection-panel.component.spec.ts for the DTO-shape side of the invariant).
describe('AbwabPermissions composite-read visibility (mirrors the backend redaction table)', () => {
  it('denies tree/search visibility when either category.view or section.view is missing', () => {
    expect(createPermissions([]).permissions.canViewTree()).toBe(false);
    expect(createPermissions(['category.view']).permissions.canViewTree()).toBe(false);
    expect(createPermissions(['section.view']).permissions.canViewTree()).toBe(false);
  });

  it('grants tree/search visibility once both category.view and section.view are present', () => {
    expect(createPermissions(['category.view', 'section.view']).permissions.canViewTree()).toBe(true);
  });

  it('withholds full protection metadata visibility without protection.view, even with tree access', () => {
    const { permissions } = createPermissions(['category.view', 'section.view']);
    expect(permissions.canViewTree()).toBe(true);
    expect(permissions.canViewProtectionDetail()).toBe(false);
  });

  it('grants full protection metadata visibility only once category.view + section.view + protection.view are all present', () => {
    const { permissions } = createPermissions(['category.view', 'section.view', 'protection.view']);
    expect(permissions.canViewProtectionDetail()).toBe(true);
  });

  it('mutation-action visibility mirrors each individual granted code', () => {
    const { permissions } = createPermissions(['category.edit', 'section.reorder', 'protection.apply']);
    expect(permissions.canEditCategory()).toBe(true);
    expect(permissions.canAddCategory()).toBe(false);
    expect(permissions.canReorderSection()).toBe(true);
    expect(permissions.canDeleteSection()).toBe(false);
    expect(permissions.canApplyProtection()).toBe(true);
    expect(permissions.canLiftProtection()).toBe(false);
  });
});
