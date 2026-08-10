import { computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AccessAuditEventPage } from '../../../core/api/generated/models/access-audit-event-page';
import { AccessAdminApi } from '../data-access/access-admin.api';
import {
  ACCESS_USER_PICKER_PAGE_SIZE,
  AccessAuditQuery,
  AccessUserSearchState,
} from '../models/access-admin.models';
import { ACCESS_ADMIN_LOAD_ERROR, failureMessage } from './access-admin-request-failure';

const defaultAuditQuery: AccessAuditQuery = { pageSize: 25 };

export class AccessAuditStore {
  private readonly pageState = signal<AccessAuditEventPage | null>(null);
  private readonly queryState = signal<AccessAuditQuery>(defaultAuditQuery);
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private readonly appendingState = signal(false);
  private readonly appendErrorState = signal<string | null>(null);
  private readonly appendedCountState = signal(0);
  private requestVersion = 0;

  readonly events = computed(() => this.pageState()?.items ?? []);
  readonly nextCursor = computed(() => this.pageState()?.nextCursor ?? null);
  readonly query = this.queryState.asReadonly();
  readonly loading = this.loadingState.asReadonly();
  readonly error = this.errorState.asReadonly();
  readonly appending = this.appendingState.asReadonly();
  readonly appendError = this.appendErrorState.asReadonly();
  readonly appendedCount = this.appendedCountState.asReadonly();

  constructor(private readonly api: AccessAdminApi) {}

  applyQuery(query: Partial<AccessAuditQuery>): void {
    this.queryState.set({ ...this.queryState(), ...query, cursor: undefined });
  }

  async load(query = this.queryState()): Promise<void> {
    const requestVersion = ++this.requestVersion;
    this.loadingState.set(true);
    this.errorState.set(null);
    this.clearAppendState();
    try {
      const response = await firstValueFrom(this.api.listAuditEvents(query));
      if (requestVersion !== this.requestVersion) {
        return;
      }
      if (!response.isSuccess || !response.data) {
        this.errorState.set(response.message ?? ACCESS_ADMIN_LOAD_ERROR);
        return;
      }
      this.pageState.set(response.data);
    } catch (error) {
      if (requestVersion === this.requestVersion) {
        this.errorState.set(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
      }
    } finally {
      if (requestVersion === this.requestVersion) {
        this.loadingState.set(false);
      }
    }
  }

  async loadNextPage(): Promise<void> {
    const cursor = this.pageState()?.nextCursor;
    if (!cursor || this.appendingState() || this.loadingState()) {
      return;
    }

    const requestVersion = this.requestVersion;
    this.appendingState.set(true);
    this.appendErrorState.set(null);
    this.appendedCountState.set(0);
    try {
      const response = await firstValueFrom(
        this.api.listAuditEvents({ ...this.queryState(), cursor }),
      );
      if (requestVersion !== this.requestVersion) {
        return;
      }
      if (!response.isSuccess || !response.data) {
        this.appendErrorState.set(response.message ?? ACCESS_ADMIN_LOAD_ERROR);
        return;
      }
      const appended = response.data.items;
      this.pageState.set({ ...response.data, items: [...this.events(), ...appended] });
      this.appendedCountState.set(appended.length);
    } catch (error) {
      if (requestVersion === this.requestVersion) {
        this.appendErrorState.set(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
      }
    } finally {
      if (requestVersion === this.requestVersion) {
        this.appendingState.set(false);
      }
    }
  }

  async findUsers(search: string): Promise<AccessUserSearchState> {
    try {
      const response = await firstValueFrom(
        this.api.listUsers({ page: 1, pageSize: ACCESS_USER_PICKER_PAGE_SIZE, search }),
      );
      if (response.isSuccess && response.data) {
        return { users: response.data.items, error: null, loading: false };
      }
      return {
        users: [],
        error: response.message ?? ACCESS_ADMIN_LOAD_ERROR,
        loading: false,
      };
    } catch (error) {
      return {
        users: [],
        error: failureMessage(error, ACCESS_ADMIN_LOAD_ERROR),
        loading: false,
      };
    }
  }

  clear(): void {
    this.requestVersion += 1;
    this.pageState.set(null);
    this.loadingState.set(false);
    this.errorState.set(null);
    this.clearAppendState();
  }

  private clearAppendState(): void {
    this.appendingState.set(false);
    this.appendErrorState.set(null);
    this.appendedCountState.set(0);
  }
}
