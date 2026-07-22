import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AsyncAction } from '../../../core/data-access/async-action';
import { ConcurrencyConflictError } from '../../../shared/concurrency/optimistic-concurrency';
import { PermissionsApi } from '../data-access/permissions.api';
import {
  PermissionAdminView,
  PermissionMutationRequest,
  PermissionTargetKind,
} from '../models/permission.models';

type LoadStatus = 'idle' | 'loading' | 'ready' | 'error';

const SUBSTRATE_GENERATION = 0;
const LOAD_ERROR = 'تعذّر تحميل الصلاحيات. يرجى المحاولة مجددًا.';
const SUBMIT_ERROR = 'تعذّر تنفيذ العملية.';
const CONFLICT_MESSAGE = 'تغيّرت حالة الصلاحية أثناء التعديل. تم تحديث القائمة، يرجى المحاولة مجددًا.';

@Injectable()
export class PermissionsFacade {
  private readonly api = inject(PermissionsApi);

  private readonly viewSignal = signal<PermissionAdminView | null>(null);
  private readonly loadStatusSignal = signal<LoadStatus>('idle');
  private readonly loadMessageSignal = signal<string | null>(null);

  readonly loadStatus = this.loadStatusSignal.asReadonly();
  readonly loadMessage = this.loadMessageSignal.asReadonly();
  readonly catalogue = computed(() => this.viewSignal()?.catalogue ?? []);
  readonly assignments = computed(() => this.viewSignal()?.assignments ?? []);
  readonly grantedAssignments = computed(() => this.assignments().filter((assignment) => assignment.isGranted));
  readonly assignableCodes = computed(() => this.catalogue().filter((entry) => entry.assignable));

  readonly grantAction = new AsyncAction<PermissionMutationRequest, void>((request) =>
    this.submit('grant', request),
  );
  readonly revokeAction = new AsyncAction<PermissionMutationRequest, void>((request) =>
    this.submit('revoke', request),
  );

  readonly submitMessage = computed(() => this.messageFor(this.grantAction) ?? this.messageFor(this.revokeAction));

  async load(): Promise<void> {
    this.loadStatusSignal.set('loading');
    try {
      const response = await firstValueFrom(this.api.list());
      if (response.isSuccess && response.data) {
        this.viewSignal.set(response.data);
        this.loadStatusSignal.set('ready');
        this.loadMessageSignal.set(null);
      } else {
        this.fail(response.message);
      }
    } catch (error) {
      this.fail(this.httpMessage(error));
    }
  }

  buildRequest(kind: PermissionTargetKind, key: string, code: string): PermissionMutationRequest {
    return {
      targetKind: kind,
      targetKey: key.trim(),
      permissionCode: code,
      expectedTimelineGeneration: SUBSTRATE_GENERATION,
      expectedVersion: this.expectedVersionFor(kind, key.trim(), code),
    };
  }

  private expectedVersionFor(kind: PermissionTargetKind, key: string, code: string): number {
    const match = this.assignments().find(
      (assignment) =>
        assignment.targetKind === kind &&
        assignment.targetKey === key &&
        assignment.permissionCode === code,
    );
    return match?.version ?? 0;
  }

  private async submit(kind: 'grant' | 'revoke', request: PermissionMutationRequest): Promise<void> {
    let response: ApiResponse<unknown>;
    try {
      const call = kind === 'grant' ? this.api.grant(request) : this.api.revoke(request);
      response = await firstValueFrom(call);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        // Reload first so the next attempt uses refreshed versions, then signal the conflict.
        await this.load();
        throw new ConcurrencyConflictError<string>(String(request.expectedVersion), 'server');
      }
      throw new Error(this.httpMessage(error) ?? SUBMIT_ERROR);
    }

    if (!response.isSuccess) {
      throw new Error(response.message ?? SUBMIT_ERROR);
    }
    await this.load();
  }

  private fail(message: string | null): void {
    this.loadStatusSignal.set('error');
    this.loadMessageSignal.set(message ?? LOAD_ERROR);
  }

  private messageFor(action: AsyncAction<PermissionMutationRequest, void>): string | null {
    const state = action.state();
    if (state.status === 'conflict') {
      return CONFLICT_MESSAGE;
    }
    if (state.status === 'error') {
      return state.error instanceof Error ? state.error.message : SUBMIT_ERROR;
    }
    return null;
  }

  private httpMessage(error: unknown): string | null {
    if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
      const body = error.error as Partial<ApiResponse<unknown>>;
      if (typeof body.message === 'string' && body.message.trim().length > 0) {
        return body.message;
      }
    }
    return null;
  }
}
