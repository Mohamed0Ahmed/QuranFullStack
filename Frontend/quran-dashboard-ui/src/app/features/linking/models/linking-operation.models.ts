import { LinkingSourceDescriptor } from './linking-source.models';
import { LinkingSourceConfiguration } from './linking-workspace.models';

export type LinkingOperationMemberOrigin = 'workspace' | 'direct-link';

export interface LinkingOperationMember {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;
  origin: LinkingOperationMemberOrigin;
  configurationRevision: number;
}
