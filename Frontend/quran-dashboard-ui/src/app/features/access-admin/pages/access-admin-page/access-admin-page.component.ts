import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  OnInit,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';

import { QD_BP_WIDE_QUERY } from '../../../../shared/layout/breakpoints';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { AccessAccountPermissionsComponent } from '../../components/access-account-permissions/access-account-permissions.component';
import { AccessChangeReviewComponent } from '../../components/access-change-review/access-change-review.component';
import { AccessLifecycleActionsComponent } from '../../components/access-lifecycle-actions/access-lifecycle-actions.component';
import { AccessUserListComponent } from '../../components/access-user-list/access-user-list.component';
import { AccessUserSummaryCardComponent } from '../../components/access-user-summary-card/access-user-summary-card.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessUserListFilters,
  AccessUserLifecycleAction,
  accessUserNameLabel,
} from '../../models/access-admin.models';
import {
  AccessAccountWorkflowSession,
  AccessAccountWorkflowState,
  AccessWorkflowDirectoryState,
  AccessWorkflowSelectedState,
} from '../../state/access-account-workflow.session';

type ReadyState = Extract<AccessAccountWorkflowState, { kind: 'ready' }>;
type SelectedReadyState = Exclude<
  AccessWorkflowSelectedState,
  { kind: 'none' | 'loading' | 'error' }
>;
const emptyDirectory: AccessWorkflowDirectoryState = {
  users: null,
  query: { page: 1, pageSize: 25 },
  loading: false,
  error: null,
};

@Component({
  selector: 'qd-access-admin-page',
  standalone: true,
  imports: [
    AccessAccountPermissionsComponent,
    AccessChangeReviewComponent,
    AccessLifecycleActionsComponent,
    AccessUserListComponent,
    AccessUserSummaryCardComponent,
    ConfirmDialogComponent,
    ExplorerPanelSkeletonComponent,
    NgTemplateOutlet,
    QdActionDirective,
    QdControlDirective,
    QdDetailsWorkspaceComponent,
    QdErrorStateComponent,
    QdFormFieldComponent,
    QdModalShellComponent,
    QdNoticeComponent,
  ],
  templateUrl: './access-admin-page.component.html',
  styleUrl: './access-admin-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessAdminPageComponent implements OnInit, OnDestroy {
  protected readonly workflow = inject(AccessAccountWorkflowSession);
  protected readonly ready = computed<ReadyState | null>(() => {
    const state = this.workflow.state();
    return state.kind === 'ready' ? state : null;
  });
  protected readonly selected = computed<SelectedReadyState | null>(() => {
    const selected = this.ready()?.selected;
    return selected &&
      selected.kind !== 'none' &&
      selected.kind !== 'loading' &&
      selected.kind !== 'error'
      ? selected
      : null;
  });
  protected readonly selectedUser = computed(() => this.selected()?.account ?? null);
  protected readonly directory = computed(() => this.ready()?.directory ?? emptyDirectory);

  protected readonly userSwitchAwaitingDiscard = signal<number | null>(null);
  protected readonly routeLeaveAwaitingDecision = signal(false);
  protected readonly userListSheetOpen = signal(false);
  protected readonly isWide = signal(true);
  protected readonly contextSearch = linkedSignal(() => this.ready()?.directory.query.search ?? '');
  protected readonly selectedIdentity = computed(() => {
    const user = this.selectedUser();
    return user === null ? '' : accessUserNameLabel(user);
  });
  protected readonly detailLayout = computed(() => {
    const selected = this.ready()?.selected;
    return selected && selected.kind !== 'none' ? 'selection' : 'no-selection';
  });
  protected readonly lifecycleActionsApply = computed(
    () => this.selected()?.actions.some((action) => action !== 'permissions') ?? false,
  );
  protected readonly acceptWouldGrantPermissions = computed(() => {
    const selected = this.selected();
    return (
      selected?.kind === 'pending' && selected.dirty && this.ready()?.catalogue.canAssign === true
    );
  });
  protected readonly reviewShowsPermissionDiff = computed(() => {
    const selected = this.selected();
    const action = selected?.preparation?.action;
    return action === 'permissions' || (action === 'accept' && selected?.dirty === true);
  });
  protected readonly mutationMessage = computed(() => {
    const message = this.ready()?.message;
    if (!message) return null;
    if (message.kind === 'success') {
      return { text: this.labels.mutationSuccess, tone: 'success' as const };
    }
    if (message.kind === 'conflict') {
      return { text: message.text || this.labels.conflictNotice, tone: 'notice' as const };
    }
    const fallback =
      message.kind === 'denied'
        ? this.labels.accessDeniedError
        : message.kind === 'conflict-reload-error'
          ? this.labels.conflictReloadError
          : this.labels.writeError;
    return { text: message.text || fallback, tone: 'error' as const };
  });

  private wideQuery?: MediaQueryList;
  private readonly onWideChange = (event: MediaQueryListEvent): void =>
    this.isWide.set(event.matches);
  private routeLeaveDecision: Promise<boolean> | null = null;
  private routeLeaveResolver: ((allowed: boolean) => void) | null = null;

  constructor() {
    void this.workflow.load();
    inject(DestroyRef).onDestroy(() => this.settleRouteLeave(false));
  }

  ngOnInit(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }
    this.wideQuery = window.matchMedia(QD_BP_WIDE_QUERY);
    this.isWide.set(this.wideQuery.matches);
    this.wideQuery.addEventListener('change', this.onWideChange);
  }

  ngOnDestroy(): void {
    this.wideQuery?.removeEventListener('change', this.onWideChange);
  }

  protected get labels(): typeof ACCESS_ADMIN_LABELS {
    return ACCESS_ADMIN_LABELS;
  }

  hasUnsavedChanges(): boolean {
    return this.ready()?.dirty ?? false;
  }

  confirmRouteLeave(): Promise<boolean> {
    if (!this.hasUnsavedChanges()) {
      return Promise.resolve(true);
    }
    if (this.routeLeaveDecision !== null) {
      return this.routeLeaveDecision;
    }
    this.routeLeaveDecision = new Promise<boolean>((resolve) => {
      this.routeLeaveResolver = resolve;
    });
    this.routeLeaveAwaitingDecision.set(true);
    return this.routeLeaveDecision;
  }

  protected allowRouteLeave(): void {
    this.settleRouteLeave(true);
  }

  protected cancelRouteLeave(): void {
    this.settleRouteLeave(false);
  }

  protected openUserListSheet(): void {
    this.userListSheetOpen.set(true);
  }

  protected updateContextSearch(event: Event): void {
    this.contextSearch.set((event.target as HTMLInputElement).value);
  }

  protected applyContextSearch(event: Event): void {
    event.preventDefault();
    const search = this.contextSearch().trim();
    void this.workflow.updateUserQuery({ search: search || undefined });
    this.openUserListSheet();
  }

  protected closeUserListSheet(): void {
    this.userListSheetOpen.set(false);
  }

  protected selectUser(userId: number): void {
    const ready = this.ready();
    if (!ready || ready.busyAction !== null) {
      return;
    }
    const selected = this.selected();
    if (selected?.account.id === userId && !selected.conflictBlocked) {
      return;
    }
    if (ready.dirty) {
      this.userSwitchAwaitingDiscard.set(userId);
      return;
    }
    this.closeUserListSheet();
    void this.workflow.selectUser(userId);
  }

  protected discardDraftAndSwitchUser(): void {
    const userId = this.userSwitchAwaitingDiscard();
    this.userSwitchAwaitingDiscard.set(null);
    if (userId === null) {
      return;
    }
    this.closeUserListSheet();
    this.workflow.discardDraft();
    void this.workflow.selectUser(userId);
  }

  protected keepEditingDraft(): void {
    this.userSwitchAwaitingDiscard.set(null);
  }

  protected updateUsers(filters: AccessUserListFilters): void {
    void this.workflow.updateUserQuery(filters);
  }

  protected updateUserPage(page: number): void {
    void this.workflow.updateUserQuery({ page });
  }

  protected updatePermissionCodes(codes: string[]): void {
    this.workflow.setPermissionCodes(new Set(codes));
  }

  protected discardDraft(): void {
    this.workflow.discardDraft();
  }

  protected reloadPermissionCatalogue(): void {
    void this.workflow.retryCatalogue();
  }

  protected requestLifecycleAction(kind: AccessUserLifecycleAction): void {
    this.workflow.prepare(kind);
  }

  protected requestPermissionSave(): void {
    this.workflow.prepare('permissions');
  }

  protected cancelPendingAction(): void {
    this.workflow.cancelPreparation();
  }

  protected confirmPendingAction(reason: string): void {
    void this.workflow.commit(reason);
  }

  private settleRouteLeave(allowed: boolean): void {
    const resolve = this.routeLeaveResolver;
    this.routeLeaveResolver = null;
    this.routeLeaveDecision = null;
    this.routeLeaveAwaitingDecision.set(false);
    resolve?.(allowed);
  }
}
