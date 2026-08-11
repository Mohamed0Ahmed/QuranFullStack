import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
import { MockLinkingCommandPort } from './mock-linking-command.port';

export interface LinkingCommand {
  doorId: number;
  operation: LinkingSourceSetOperationResult;
}

export interface LinkingCommandPort {
  execute(command: LinkingCommand): Observable<{ kind: 'linked'; message: string }>;
}

export const LINKING_COMMAND_PORT = new InjectionToken<LinkingCommandPort>('LINKING_COMMAND_PORT', {
  providedIn: 'root',
  factory: () => inject(MockLinkingCommandPort),
});
