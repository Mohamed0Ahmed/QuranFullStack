import { InjectionToken } from '@angular/core';

import {
  AddDoorTemplateRequest,
  AddTemplateNodeAliasRequest,
  AddTemplateNodeRequest,
  ApplyDoorTemplateRequest,
  DeleteDoorTemplateRequest,
  DoorTemplateDetailDto,
  DoorTemplateListDto,
  EditDoorTemplateRequest,
  EditTemplateNodeAliasRequest,
  EditTemplateNodeRequest,
  RemoveTemplateNodeAliasRequest,
  RemoveTemplateNodeRequest,
  ReorderTemplateNodesRequest,
  ReparentTemplateNodeRequest,
  RestoreDoorTemplateRequest,
  RestoreTemplateNodeAliasRequest,
  TemplateHistoryDto,
} from '../../../core/api/generated/models';

export interface AddDoorTemplateResult {
  readonly doorTemplateId: string;
}

export interface AddTemplateNodeResult {
  readonly templateNodeId: string;
}

export interface AddTemplateNodeAliasResult {
  readonly templateNodeSearchAliasId: string;
}

// The template domain gets its own port (§14.1), like relationships — never an extension of
// AbwabCorePort. Reads carry the server TimelineGeneration; a mutation result never synthesizes one.
export interface AbwabTemplatesPort {
  getTemplates(includeDeleted?: boolean): Promise<DoorTemplateListDto>;
  getTemplate(doorTemplateId: string, includeDeleted?: boolean): Promise<DoorTemplateDetailDto>;
  getHistory(doorTemplateId: string): Promise<TemplateHistoryDto>;

  addTemplate(request: AddDoorTemplateRequest): Promise<AddDoorTemplateResult>;
  editTemplate(doorTemplateId: string, request: EditDoorTemplateRequest): Promise<void>;
  deleteTemplate(doorTemplateId: string, request: DeleteDoorTemplateRequest): Promise<void>;
  restoreTemplate(doorTemplateId: string, request: RestoreDoorTemplateRequest): Promise<void>;

  addNode(doorTemplateId: string, request: AddTemplateNodeRequest): Promise<AddTemplateNodeResult>;
  editNode(templateNodeId: string, request: EditTemplateNodeRequest): Promise<void>;
  reparentNode(templateNodeId: string, request: ReparentTemplateNodeRequest): Promise<void>;
  reorderNodes(doorTemplateId: string, request: ReorderTemplateNodesRequest): Promise<void>;
  removeNode(templateNodeId: string, request: RemoveTemplateNodeRequest): Promise<void>;

  addNodeAlias(templateNodeId: string, request: AddTemplateNodeAliasRequest): Promise<AddTemplateNodeAliasResult>;
  editNodeAlias(aliasId: string, request: EditTemplateNodeAliasRequest): Promise<void>;
  removeNodeAlias(aliasId: string, request: RemoveTemplateNodeAliasRequest): Promise<void>;
  restoreNodeAlias(aliasId: string, request: RestoreTemplateNodeAliasRequest): Promise<void>;

  applyTemplate(doorTemplateId: string, request: ApplyDoorTemplateRequest): Promise<void>;
}

export const ABWAB_TEMPLATES_PORT = new InjectionToken<AbwabTemplatesPort>('ABWAB_TEMPLATES_PORT');
