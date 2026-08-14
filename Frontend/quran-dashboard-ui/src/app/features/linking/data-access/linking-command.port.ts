import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingConfirmationResultDto } from '../../../core/api/generated/models/linking-confirmation-result-dto';
import { LinkingSourcePreflight } from '../models/linking-preflight.models';
import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
export { LinkingDataStaleError } from '../models/linking-revision.models';
import { HttpLinkingCommandPort } from './http-linking-command.port';

export interface LinkingCommand {
  doorId: number;
  operation: LinkingSourceSetOperationResult;
  preflightToken: string;
  expectedLinkingDataRevision: number;
  idempotencyKey: string;
  preflightSources: readonly LinkingSourcePreflight[];
}

export interface LinkingCommandResult {
  kind: 'linked';
  message: string;
  result: LinkingConfirmationResultDto;
}

export interface LinkingCommandPort {
  execute(command: LinkingCommand): Observable<LinkingCommandResult>;
}

export class LinkingPreflightStaleError extends Error {
  constructor(message: string) {
    super(message);
  }
}

export const LINKING_COMMAND_PORT = new InjectionToken<LinkingCommandPort>('LINKING_COMMAND_PORT', {
  providedIn: 'root',
  factory: () => inject(HttpLinkingCommandPort),
});
