import { InjectionToken } from '@angular/core';

import {
  AddRelationshipRequest,
  CategoryRelationshipListDto,
  DeleteRelationshipRequest,
  EditRelationshipRequest,
  RelationshipType,
  RestoreRelationshipRequest,
} from '../../../core/api/generated/models';

export const RELATIONSHIP_TYPE_SIMILAR = 0 satisfies RelationshipType;
export const RELATIONSHIP_TYPE_OPPOSITE = 1 satisfies RelationshipType;
export const RELATIONSHIP_TYPE_BROADER_NARROWER = 2 satisfies RelationshipType;

export const RELATIONSHIP_TYPES = [
  RELATIONSHIP_TYPE_SIMILAR,
  RELATIONSHIP_TYPE_OPPOSITE,
  RELATIONSHIP_TYPE_BROADER_NARROWER,
] as const satisfies readonly RelationshipType[];

export interface AddRelationshipResult {
  readonly relationshipId: string;
}

// The relationship domain gets its own port (§14.1) rather than extending AbwabCorePort.
export interface AbwabRelationshipsPort {
  getRelationships(categoryId: string, includeDeleted?: boolean): Promise<CategoryRelationshipListDto>;

  addRelationship(request: AddRelationshipRequest): Promise<AddRelationshipResult>;
  editRelationship(relationshipId: string, request: EditRelationshipRequest): Promise<void>;
  deleteRelationship(relationshipId: string, request: DeleteRelationshipRequest): Promise<void>;
  restoreRelationship(relationshipId: string, request: RestoreRelationshipRequest): Promise<void>;
}

export const ABWAB_RELATIONSHIPS_PORT = new InjectionToken<AbwabRelationshipsPort>('ABWAB_RELATIONSHIPS_PORT');
