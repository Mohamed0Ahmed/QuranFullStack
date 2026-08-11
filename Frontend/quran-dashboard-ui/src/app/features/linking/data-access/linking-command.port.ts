import { InjectionToken, inject } from '@angular/core';

import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { DirectLinkResult } from '../models/linking-workflow.models';
import { MockLinkingCommandPort } from './mock-linking-command.port';

export interface LinkingCommand {
  source: LinkingSourceDescriptor;
  doorId: number;
  selectedVerseKeys: readonly string[];
  highlightSourceWords: boolean;
}

export interface LinkingCommandPort {
  execute(command: LinkingCommand): DirectLinkResult;
}

export const LINKING_COMMAND_PORT = new InjectionToken<LinkingCommandPort>('LINKING_COMMAND_PORT', {
  providedIn: 'root',
  factory: () => inject(MockLinkingCommandPort),
});
