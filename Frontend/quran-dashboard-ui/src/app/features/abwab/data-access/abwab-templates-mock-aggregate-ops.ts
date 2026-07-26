import {
  AddDoorTemplateRequest,
  DeleteDoorTemplateRequest,
  EditDoorTemplateRequest,
  RestoreDoorTemplateRequest,
} from '../../../core/api/generated/models';
import {
  AbwabTemplatesMockState,
  assertGeneration,
  requireTemplateVersion,
} from './abwab-templates-mock-state';

export function addTemplate(state: AbwabTemplatesMockState, idFactory: () => string, request: AddDoorTemplateRequest): string {
  assertGeneration(state, request.expectedTimelineGeneration);

  const doorTemplateId = idFactory();
  state.templates.set(doorTemplateId, {
    doorTemplateId,
    name: request.name,
    description: request.description,
    templateRevision: 0,
    isDeleted: false,
    version: 1,
  });
  return doorTemplateId;
}

export function editTemplate(state: AbwabTemplatesMockState, doorTemplateId: string, request: EditDoorTemplateRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const template = requireTemplateVersion(state, doorTemplateId, request.expectedVersion, { isDeleted: false });

  template.name = request.name;
  template.description = request.description;
  template.version += 1;
}

export function deleteTemplate(state: AbwabTemplatesMockState, doorTemplateId: string, request: DeleteDoorTemplateRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const template = requireTemplateVersion(state, doorTemplateId, request.expectedVersion, { isDeleted: false });

  template.isDeleted = true;
  template.version += 1;
}

export function restoreTemplate(state: AbwabTemplatesMockState, doorTemplateId: string, request: RestoreDoorTemplateRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const template = requireTemplateVersion(state, doorTemplateId, request.expectedVersion, { isDeleted: true });

  template.isDeleted = false;
  template.version += 1;
}
