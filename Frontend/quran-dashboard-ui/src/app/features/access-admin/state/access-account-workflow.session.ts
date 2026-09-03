import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { PERMISSION_CODES, PermissionCode, isPermissionCode } from '../../../core/auth/permission-code';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AccessAdminApi } from '../data-access/access-admin.api';
import { AccessPermissionDiff, AccessUserListQuery, AccessUserWorkflowAction } from '../models/access-admin.models';
import { AccessPermissionGroup, buildPermissionGroups, permissionCodesForSubmission } from '../models/access-admin-permissions';

interface Message {
  readonly kind: 'success' | 'conflict' | 'conflict-reload-error' | 'denied' | 'error';
  readonly text: string | null;
}
export interface AccessWorkflowDirectoryState {
  readonly users: AccessUserSummaryPagedResult | null;
  readonly query: AccessUserListQuery;
  readonly loading: boolean;
  readonly error: string | null;
}
interface CatalogueState {
  readonly groups: readonly AccessPermissionGroup[];
  readonly loading: boolean;
  readonly error: string | null;
  readonly canAssign: boolean;
  readonly generation: number;
}
interface Preparation {
  readonly action: AccessUserWorkflowAction;
  readonly targetId: number;
  readonly targetGeneration: number;
  readonly version: number;
  readonly catalogueGeneration: number | null;
}

interface SelectedReadyBase {
  readonly account: AccessUserDetail;
  readonly targetGeneration: number;
  readonly selectedCodes: ReadonlySet<PermissionCode>;
  readonly unknownPermissionCodes: readonly string[];
  readonly permissionDiff: AccessPermissionDiff;
  readonly dirty: boolean;
  readonly canEditPermissions: boolean;
  readonly preparation: Preparation | null;
  readonly conflictBlocked: boolean;
}
type ReadySelected = SelectedReadyBase & {
  readonly actions: readonly AccessUserWorkflowAction[];
} & (
    | { readonly kind: 'owner'; readonly mode: 'owner' }
    | { readonly kind: 'pending' | 'active'; readonly mode: 'permissions' }
    | { readonly kind: 'disabled'; readonly mode: 'disabled' }
    | { readonly kind: 'unknown'; readonly mode: 'unknown' }
  );
export type AccessWorkflowSelectedState =
  | { readonly kind: 'none' }
  | { readonly kind: 'loading'; readonly targetId: number; readonly targetGeneration: number }
  | {
      readonly kind: 'error';
      readonly targetId: number;
      readonly targetGeneration: number;
      readonly message: string;
    }
  | ReadySelected;
interface ReadyPage {
  readonly kind: 'ready';
  readonly directory: AccessWorkflowDirectoryState;
  readonly catalogue: CatalogueState;
  readonly selected: AccessWorkflowSelectedState;
  readonly message: Message | null;
  readonly busyAction: AccessUserWorkflowAction | null;
  readonly dirty: boolean;
}
export type AccessAccountWorkflowState = { readonly kind: 'checking' } | { readonly kind: 'denied' } | ReadyPage;
type AcceptedSeed = Pick<SelectedReadyBase, 'account' | 'targetGeneration' | 'selectedCodes' | 'preparation' | 'conflictBlocked'>;
type SelectionInput = AccessWorkflowSelectedState | AcceptedSeed;
type Concern = 'directory' | 'catalogue' | 'detail' | 'mutation';
type Fence = readonly [epoch: number, concern: Concern, token: number];

const defaultQuery: AccessUserListQuery = { page: 1, pageSize: 25 };
const emptyDirectory: AccessWorkflowDirectoryState = {
  users: null,
  query: defaultQuery,
  loading: false,
  error: null,
};
const emptyCatalogue: CatalogueState = {
  groups: [],
  loading: false,
  error: null,
  canAssign: false,
  generation: 0,
};
const emptyPage: ReadyPage = {
  kind: 'ready',
  directory: emptyDirectory,
  catalogue: emptyCatalogue,
  selected: { kind: 'none' },
  message: null,
  busyAction: null,
  dirty: false,
};
@Injectable()
export class AccessAccountWorkflowSession {
  private readonly api = inject(AccessAdminApi);
  private readonly auth = inject(AuthSessionStore);
  private readonly page = signal<ReadyPage>(emptyPage);
  private readonly tokens: Record<Concern, number> = {
    directory: 0,
    catalogue: 0,
    detail: 0,
    mutation: 0,
  };
  private targetGeneration = 0;
  private epoch = 0;
  private destroyed = false;

  readonly state = computed<AccessAccountWorkflowState>(() => {
    if (this.auth.isResolving()) return { kind: 'checking' };
    return this.auth.isActiveOwner() ? this.page() : { kind: 'denied' };
  });

  constructor() {
    effect(() => {
      if (!this.auth.isActiveOwner()) untracked(() => this.invalidateProtectedState());
    });
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.invalidateProtectedState();
    });
  }

  async load(): Promise<void> {
    await this.auth.ensureResolved();
    if (!this.authorized()) return this.invalidateProtectedState();
    await Promise.all([this.loadDirectory(), this.loadCatalogue()]);
  }

  async updateUserQuery(query: Partial<AccessUserListQuery>): Promise<void> {
    if (!this.authorized()) return;
    const current = this.page().directory;
    this.updatePage({
      directory: { ...current, query: { ...current.query, ...query, page: query.page ?? 1 } },
    });
    await this.loadDirectory();
  }

  async selectUser(userId: number): Promise<void> {
    const page = this.page();
    if (!this.authorized() || page.busyAction !== null || page.dirty) return;
    if (isReady(page.selected) && page.selected.account.id === userId && !page.selected.conflictBlocked) return;
    const fence = this.start('detail');
    const targetGeneration = ++this.targetGeneration;
    this.updatePage({ message: null }, { kind: 'loading', targetId: userId, targetGeneration });
    try {
      const response = await firstValueFrom(this.api.getUser(userId));
      if (!this.current(fence)) return;
      if (response.isSuccess && response.data?.id === userId) {
        this.updatePage({}, freshSelection(response.data, targetGeneration));
      } else this.selectError(userId, targetGeneration, response.message ?? '');
    } catch (error: unknown) {
      if (this.current(fence)) {
        this.selectError(userId, targetGeneration, errorMessage(error, ''));
      }
    }
  }

  setPermissionCodes(codes: ReadonlySet<string>): void {
    const page = this.page();
    const selected = page.selected;
    if (!isReady(selected) || !selected.canEditPermissions) return;
    const offered = new Set(page.catalogue.groups.flatMap((group) => group.codes));
    const draft = new Set(PERMISSION_CODES.filter((code) => (offered.has(code) ? codes.has(code) : selected.selectedCodes.has(code))));
    this.reviseSelected({ selectedCodes: draft });
  }

  discardDraft(): void {
    const page = this.page();
    if (!isReady(page.selected) || page.busyAction !== null) return;
    const selectedCodes = knownCodes(page.selected.account.permissionCodes);
    this.reviseSelected({ selectedCodes, preparation: null });
  }

  prepare(action: AccessUserWorkflowAction): void {
    const page = this.page();
    const selected = page.selected;
    if (!isReady(selected) || page.busyAction !== null || !selected.actions.includes(action)) return;
    const preparation: Preparation = {
      action,
      targetId: selected.account.id,
      targetGeneration: selected.targetGeneration,
      version: selected.account.version,
      catalogueGeneration: action === 'permissions' || (action === 'accept' && selected.dirty) ? page.catalogue.generation : null,
    };
    this.reviseSelected({ preparation });
  }

  cancelPreparation(): void {
    const page = this.page();
    if (isReady(page.selected) && page.busyAction === null) {
      this.reviseSelected({ preparation: null });
    }
  }

  async commit(reason: string): Promise<void> {
    const page = this.page();
    const initial = page.selected;
    if (!isReady(initial) || page.busyAction !== null || initial.preparation === null) return;
    const preparation = initial.preparation;
    const fence = this.start('mutation');
    this.updatePage({ busyAction: preparation.action, message: null });
    try {
      const response = await firstValueFrom(this.mutationCall(preparation.action, initial.account, submissionCodes(initial), reason.trim() || null));
      if (!this.mutationIsCurrent(fence, preparation)) return;
      if (!response.isSuccess || !response.data) {
        this.finishMutation('error', response.message);
        return;
      }
      const account = adoptResponse(initial, preparation.action, response.data);
      if (account === null) {
        this.finishMutation('error', null);
        return;
      }
      this.updatePage({ busyAction: null, message: { kind: 'success', text: null } }, freshSelection(account, initial.targetGeneration));
      void this.loadDirectory();
    } catch (error: unknown) {
      if (!this.mutationIsCurrent(fence, preparation)) return;
      if (error instanceof HttpErrorResponse && error.status === 409) {
        await this.recoverConflict(error, fence, preparation);
        return;
      }
      const authFailure = await this.auth.handleWriteAuthFailure(error);
      if (!this.mutationIsCurrent(fence, preparation)) return;
      this.finishMutation(authFailure ? 'denied' : 'error', errorMessage(error, null));
    }
  }

  async retryCatalogue(): Promise<void> {
    await this.loadCatalogue();
  }

  private async loadDirectory(): Promise<void> {
    if (!this.authorized()) return;
    const fence = this.start('directory');
    const previous = this.page().directory;
    const query = previous.query;
    this.updatePage({ directory: { ...previous, loading: true, error: null } });
    try {
      const response = await firstValueFrom(this.api.listUsers(query));
      if (!this.current(fence)) return;
      this.updatePage({
        directory: {
          users: response.isSuccess && response.data ? response.data : previous.users,
          query,
          loading: false,
          error: response.isSuccess && response.data ? null : (response.message ?? ''),
        },
      });
    } catch (error: unknown) {
      if (this.current(fence)) {
        this.updatePage({
          directory: {
            ...this.page().directory,
            loading: false,
            error: errorMessage(error, ''),
          },
        });
      }
    }
  }

  private async loadCatalogue(): Promise<void> {
    if (!this.authorized()) return;
    const fence = this.start('catalogue');
    const previous = this.page().catalogue;
    const generation = previous.generation + 1;
    this.updatePage({
      catalogue: { ...previous, loading: true, error: null, canAssign: false, generation },
    });
    try {
      const response = await firstValueFrom(this.api.getPermissionCatalogue());
      if (!this.current(fence)) return;
      if (!response.isSuccess || !response.data) {
        this.failCatalogue(response.message ?? '');
        return;
      }
      const groups = buildPermissionGroups(response.data.items);
      this.updatePage({
        catalogue: {
          groups,
          loading: false,
          error: null,
          canAssign: response.data.assignmentReady && groups.length > 0,
          generation,
        },
      });
    } catch (error: unknown) {
      if (this.current(fence)) this.failCatalogue(errorMessage(error, ''));
    }
  }

  private async recoverConflict(error: HttpErrorResponse, fence: Fence, preparation: Preparation): Promise<void> {
    const selected = this.page().selected;
    if (!isReady(selected)) return;
    this.reviseSelected({ preparation: null });
    let reloadError: string | null = null;
    try {
      const response = await firstValueFrom(this.api.getUser(preparation.targetId));
      if (!this.mutationIsCurrent(fence, preparation)) return;
      if (!response.isSuccess || response.data?.id !== preparation.targetId) {
        reloadError = response.message;
        throw new Error();
      }
      this.updatePage(
        {
          busyAction: null,
          message: {
            kind: 'conflict',
            text: errorMessage(error, null),
          },
        },
        freshSelection(response.data, preparation.targetGeneration),
      );
    } catch (readError: unknown) {
      if (!this.mutationIsCurrent(fence, preparation)) return;
      const current = this.page().selected;
      if (isReady(current)) {
        this.reviseSelected({ preparation: null, conflictBlocked: true });
      }
      this.finishMutation('conflict-reload-error', reloadError ?? errorMessage(readError, null));
    }
  }

  private mutationCall(
    action: AccessUserWorkflowAction,
    account: AccessUserDetail,
    permissionCodes: string[],
    reason: string | null,
  ): Observable<ApiResponse<AccessUserDetail | AccessUserPermissions>> {
    const expectedVersion = account.version;
    if (action === 'accept') return this.api.acceptUser(account.id, { expectedVersion, permissionCodes, reason });
    if (action === 'disable') return this.api.disableUser(account.id, { expectedVersion, reason });
    if (action === 'reactivate') return this.api.reactivateUser(account.id, { expectedVersion, reason });
    return this.api.replacePermissions(account.id, { expectedVersion, permissionCodes, reason });
  }

  private updatePage(change: Partial<Omit<ReadyPage, 'kind' | 'selected' | 'dirty'>>, input?: SelectionInput): void {
    this.page.update((current) => {
      const next = { ...current, ...change };
      const candidate = input ?? current.selected;
      const selected = 'account' in candidate ? buildSelected(candidate, next.catalogue, next.busyAction) : candidate;
      return { ...next, selected, dirty: isReady(selected) && selected.dirty };
    });
  }

  private reviseSelected(change: Partial<AcceptedSeed>): void {
    const selected = this.page().selected;
    if (isReady(selected)) this.updatePage({}, { ...selected, ...change });
  }
  private selectError(targetId: number, targetGeneration: number, message: string): void {
    this.updatePage({}, { kind: 'error', targetId, targetGeneration, message });
  }

  private start(concern: Concern): Fence {
    return [this.epoch, concern, ++this.tokens[concern]];
  }
  private current(fence: Fence): boolean {
    return this.authorized() && fence[0] === this.epoch && fence[2] === this.tokens[fence[1]];
  }

  private mutationIsCurrent(fence: Fence, preparation: Preparation): boolean {
    const page = this.page();
    const selected = page.selected;
    return (
      this.current(fence) &&
      isReady(selected) &&
      selected.account.id === preparation.targetId &&
      selected.targetGeneration === preparation.targetGeneration &&
      selected.account.version === preparation.version &&
      page.busyAction === preparation.action
    );
  }
  private finishMutation(kind: Message['kind'], text: string | null): void {
    this.updatePage({ busyAction: null, message: { kind, text } });
  }

  private failCatalogue(error: string): void {
    this.updatePage({
      catalogue: { ...this.page().catalogue, loading: false, error, canAssign: false },
    });
  }
  private authorized(): boolean {
    return !this.destroyed && this.auth.isActiveOwner();
  }

  private invalidateProtectedState(): void {
    this.epoch++;
    this.page.set(emptyPage);
  }
}
function freshSelection(detail: AccessUserDetail, targetGeneration: number): AcceptedSeed {
  const account = {
    ...detail,
    permissionCodes: [...PERMISSION_CODES.filter((code) => detail.permissionCodes.includes(code)), ...unknownCodes(detail.permissionCodes)],
  };
  return {
    account,
    targetGeneration,
    selectedCodes: knownCodes(account.permissionCodes),
    preparation: null,
    conflictBlocked: false,
  };
}
function buildSelected(seed: AcceptedSeed, catalogue: CatalogueState, busyAction: AccessUserWorkflowAction | null): ReadySelected {
  const account = seed.account;
  const baseline = knownCodes(account.permissionCodes);
  const permissionDiff = diff(baseline, seed.selectedCodes);
  const dirty = permissionDiff.granted.length > 0 || permissionDiff.revoked.length > 0;
  let actions: readonly AccessUserWorkflowAction[] = [];
  if (!account.isOwner && !seed.conflictBlocked) {
    if (account.status === 'pending') actions = !dirty || catalogue.canAssign ? ['accept'] : [];
    if (account.status === 'active') {
      actions = catalogue.canAssign && dirty ? ['disable', 'permissions'] : ['disable'];
    }
    if (account.status === 'disabled') actions = ['reactivate'];
  }
  let preparation = seed.preparation;
  const catalogueDependent = preparation?.action === 'permissions' || (preparation?.action === 'accept' && dirty);
  const staleCatalogue = preparation?.catalogueGeneration != null && preparation.catalogueGeneration !== catalogue.generation;
  if (
    preparation === null ||
    seed.conflictBlocked ||
    preparation.targetId !== account.id ||
    preparation.targetGeneration !== seed.targetGeneration ||
    preparation.version !== account.version ||
    !actions.includes(preparation.action) ||
    (catalogueDependent && (!catalogue.canAssign || staleCatalogue))
  ) {
    preparation = null;
  } else {
    preparation = {
      ...preparation,
      catalogueGeneration: catalogueDependent ? catalogue.generation : null,
    };
  }
  const common: SelectedReadyBase = {
    account,
    targetGeneration: seed.targetGeneration,
    selectedCodes: seed.selectedCodes,
    unknownPermissionCodes: unknownCodes(account.permissionCodes),
    permissionDiff,
    dirty,
    canEditPermissions: busyAction === null && !seed.conflictBlocked && catalogue.canAssign && canEdit(account),
    preparation,
    conflictBlocked: seed.conflictBlocked,
  };
  if (account.status !== 'pending' && account.status !== 'active' && account.status !== 'disabled') {
    return { ...common, kind: 'unknown', mode: 'unknown', actions: [] };
  }
  if (account.isOwner) return { ...common, kind: 'owner', mode: 'owner', actions: [] };
  if (account.status === 'pending') return { ...common, kind: 'pending', mode: 'permissions', actions };
  if (account.status === 'active') return { ...common, kind: 'active', mode: 'permissions', actions };
  if (account.status === 'disabled') {
    return { ...common, kind: 'disabled', mode: 'disabled', actions };
  }
  return { ...common, kind: 'unknown', mode: 'unknown', actions: [] };
}
function adoptResponse(selected: ReadySelected, action: AccessUserWorkflowAction, response: AccessUserDetail | AccessUserPermissions): AccessUserDetail | null {
  if (action !== 'permissions') {
    const detail = response as AccessUserDetail;
    return detail.id === selected.account.id ? detail : null;
  }
  const permissions = response as AccessUserPermissions;
  return permissions.userId === selected.account.id
    ? {
        ...selected.account,
        status: permissions.status,
        isOwner: permissions.isOwner,
        version: permissions.version,
        permissionCodes: permissions.permissionCodes,
      }
    : null;
}
function knownCodes(codes: readonly string[]): ReadonlySet<PermissionCode> {
  return new Set(PERMISSION_CODES.filter((code) => codes.includes(code)));
}

function unknownCodes(codes: readonly string[]): string[] {
  return [...new Set(codes.filter((code) => !isPermissionCode(code)))];
}
function diff(baseline: ReadonlySet<PermissionCode>, draft: ReadonlySet<PermissionCode>): AccessPermissionDiff {
  return {
    granted: PERMISSION_CODES.filter((code) => draft.has(code) && !baseline.has(code)),
    revoked: PERMISSION_CODES.filter((code) => baseline.has(code) && !draft.has(code)),
  };
}
function canEdit(account: AccessUserDetail): boolean {
  return !account.isOwner && (account.status === 'pending' || account.status === 'active');
}

function submissionCodes(selected: ReadySelected): string[] {
  return [...permissionCodesForSubmission(selected.selectedCodes), ...selected.unknownPermissionCodes];
}
function isReady(selected: AccessWorkflowSelectedState): selected is ReadySelected {
  return selected.kind !== 'none' && selected.kind !== 'loading' && selected.kind !== 'error';
}

function errorMessage(error: unknown, fallback: string): string;
function errorMessage(error: unknown, fallback: null): string | null;
function errorMessage(error: unknown, fallback: string | null): string | null {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) return fallback;
  const response = error.error as Partial<ApiResponse<unknown>>;
  return typeof response.message === 'string' && response.message.trim() ? response.message : fallback;
}
