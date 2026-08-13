import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { LinkingConfirmationResultDto } from '../../../core/api/generated/models/linking-confirmation-result-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LINKING_LABELS } from '../models/linking.labels';
import {
  LinkingCommand,
  LinkingCommandPort,
  LinkingCommandResult,
  LinkingPreflightStaleError,
} from './linking-command.port';
import { toPreflightSourceBodies } from './linking-operation-request';

@Injectable({ providedIn: 'root' })
export class HttpLinkingCommandPort implements LinkingCommandPort {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  execute(command: LinkingCommand): Observable<LinkingCommandResult> {
    return this.http
      .post<ApiResponse<LinkingConfirmationResultDto>>(`${this.baseUrl}/api/linking/operations`, {
        doorId: command.doorId,
        preflightToken: command.preflightToken,
        idempotencyKey: command.idempotencyKey,
        sources: toConfirmationSourceBodies(command),
      })
      .pipe(
        map((response) => toCommandResult(response)),
        catchError((error: unknown) => throwError(() => toCommandError(error))),
      );
  }
}

function toConfirmationSourceBodies(command: LinkingCommand) {
  return toPreflightSourceBodies(command.operation.sourceIntents).map((source, index) => {
    const existing = command.preflightSources[index];
    return {
      ...source,
      existingContributionId: existing?.existingContributionId ?? null,
      existingContributionVersion: existing?.existingContributionVersion ?? null,
    };
  });
}

function toCommandResult(response: ApiResponse<LinkingConfirmationResultDto>): LinkingCommandResult {
  const result = response.data;
  if (!response.isSuccess || !result) {
    throw new Error(response.message || LINKING_LABELS.sourceLoadError);
  }
  return { kind: 'linked', message: response.message || LINKING_LABELS.success, result };
}

function toCommandError(error: unknown): Error {
  if (error instanceof HttpErrorResponse) {
    const message = (error.error as ApiResponse<unknown> | null)?.message;
    if (error.status === 409) {
      return new LinkingPreflightStaleError(message || LINKING_LABELS.preflightStale);
    }
    return new Error(message || LINKING_LABELS.sourceLoadError);
  }
  return error instanceof Error ? error : new Error(LINKING_LABELS.sourceLoadError);
}
