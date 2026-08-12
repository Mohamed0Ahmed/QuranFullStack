import { LinkingSourceDescriptor } from './linking-source.models';
import { LinkingSourceConfiguration, LinkingWorkspaceDescription } from './linking-workspace.models';

export type LinkingOperationMemberOrigin = 'workspace' | 'direct-link';

export interface LinkingOperationMember {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;
  descriptions: readonly LinkingWorkspaceDescription[];
  origin: LinkingOperationMemberOrigin;
  configurationRevision: number;
  operationOrder: number;
}

export type LinkingOperationMemberLoadStatus = 'unresolved' | 'loading' | 'error' | 'ready';

export interface LinkingOperationMemberLoadState {
  member: LinkingOperationMember;
  status: LinkingOperationMemberLoadStatus;
  progress: { loaded: number; total: number | null };
  errorMessage: string | null;
  contributedAyahCount: number | null;
}
