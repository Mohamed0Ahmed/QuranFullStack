import { LinkingOperationMember } from '../models/linking-operation.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceConfiguration } from '../models/linking-workspace.models';

export function ephemeralLinkingOperationMember(
  source: LinkingSourceDescriptor,
  configuration: LinkingSourceConfiguration,
): LinkingOperationMember {
  return {
    sourceKey: `ephemeral:${source.kind}`,
    source,
    configuration,
    descriptions: [],
    origin: 'direct-link',
    configurationRevision: 0,
    operationOrder: 0,
  };
}

export function orderedOperationMembers(members: readonly LinkingOperationMember[]): readonly LinkingOperationMember[] {
  return [...members].sort((left, right) => left.operationOrder - right.operationOrder);
}
