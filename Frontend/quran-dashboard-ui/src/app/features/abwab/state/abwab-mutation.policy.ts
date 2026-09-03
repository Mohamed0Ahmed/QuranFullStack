import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, defer, from, map, of } from 'rxjs';

import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { PermissionCode } from '../../../core/auth/permission-code';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../models/abwab.labels';

export const ACTIVE_OWNER = Symbol('active-owner');
export type AbwabMutationAccess = PermissionCode | typeof ACTIVE_OWNER;

interface AbwabMutationResult<T> {
  readonly envelope: ApiResponse<T> | null;
  readonly message: string | null;
  readonly data: T | null;
  readonly conflictCode: string | null;
}
export type AbwabMutationFailure<T = unknown> = AbwabMutationResult<T> & {
  readonly kind: 'invalid' | 'conflict' | 'unauthorized' | 'forbidden' | 'error';
  readonly message: string;
};
export type AbwabMutationOutcome<T> =
  | (AbwabMutationResult<T> & { readonly kind: 'success' })
  | AbwabMutationFailure<T>;

@Injectable({ providedIn: 'root' })
export class AbwabMutationPolicy {
  private readonly authSession = inject(AuthSessionStore);

  execute<T>(access: AbwabMutationAccess, request: () => Observable<ApiResponse<T> | null>) {
    return defer((): Observable<AbwabMutationOutcome<T>> => {
      if (access === ACTIVE_OWNER ? !this.authSession.isActiveOwner() : !this.authSession.can(access)) {
        return of(failure<T>('forbidden', null, ABWAB_LABELS.writePermissionDenied));
      }
      return defer(request).pipe(
        map((envelope) => envelope === null || envelope.isSuccess
          ? { kind: 'success' as const, ...result(envelope) }
          : failure('invalid', envelope, ABWAB_LABELS.writeInvalidFallback)),
        catchError((error: unknown) => {
          const recovery = error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)
            ? this.authSession.handleWriteAuthFailure(error)
            : Promise.resolve(null);
          return from(recovery).pipe(
            catchError(() => of(null)),
            map(() => classifyFailure<T>(error)),
          );
        }),
      );
    });
  }
}

function classifyFailure<T>(error: unknown): AbwabMutationFailure<T> {
  if (!(error instanceof HttpErrorResponse)) {
    return failure('error', null, ABWAB_LABELS.writeTransportFallback);
  }
  const envelope = errorEnvelope<T>(error);
  const kind = error.status === 409 ? 'conflict'
    : error.status === 400 || error.status === 404 ? 'invalid'
      : error.status === 401 ? 'unauthorized'
        : error.status === 403 ? 'forbidden' : 'error';
  const fallback = kind === 'conflict' ? ABWAB_LABELS.writeConflictFallback
    : kind === 'invalid' ? ABWAB_LABELS.writeInvalidFallback
      : kind === 'error' ? ABWAB_LABELS.writeTransportFallback : ABWAB_LABELS.writePermissionDenied;
  return failure(kind, envelope, fallback);
}

function failure<T>(kind: AbwabMutationFailure<T>['kind'], envelope: ApiResponse<T> | null, fallback: string) {
  const normalized = result(envelope, kind === 'conflict');
  return { ...normalized, kind, message: normalized.message ?? fallback };
}

function result<T>(envelope: ApiResponse<T> | null, conflict = false): AbwabMutationResult<T> {
  const message = typeof envelope?.message === 'string' && envelope.message.trim() ? envelope.message : null;
  const data = envelope?.data ?? null;
  const conflictCode = conflict && typeof data === 'object' && data !== null && 'code' in data
    && typeof data.code === 'string' && data.code ? data.code : null;
  return { envelope, message, data, conflictCode };
}

function errorEnvelope<T>(error: HttpErrorResponse): ApiResponse<T> | null {
  const envelope = typeof error.error === 'object' && error.error !== null ? error.error : null;
  return typeof envelope?.isSuccess === 'boolean' ? envelope as ApiResponse<T> : null;
}
