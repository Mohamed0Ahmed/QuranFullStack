import {
  AddDoorTemplateRequest,
  AddTemplateNodeAliasRequest,
  AddTemplateNodeRequest,
  ApplyDoorTemplateRequest,
  DeleteDoorTemplateRequest,
  DoorTemplateDetailDto,
  DoorTemplateListDto,
  DoorTemplateSummaryDto,
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
  TemplateNodeAliasDto,
  TemplateNodeDto,
} from '../../../core/api/generated/models';
import {
  AbwabTemplatesPort,
  AddDoorTemplateResult,
  AddTemplateNodeAliasResult,
  AddTemplateNodeResult,
} from './abwab-templates.port';
import * as aggregateOps from './abwab-templates-mock-aggregate-ops';
import * as aliasOps from './abwab-templates-mock-alias-ops';
import * as applyOps from './abwab-templates-mock-apply-ops';
import * as nodeOps from './abwab-templates-mock-node-ops';
import {
  AbwabTemplatesMockState,
  MockTemplateNodeRow,
  MockTemplateRow,
  requireTemplate,
} from './abwab-templates-mock-state';

export interface AbwabTemplatesMockOptions {
  readonly targetCategoryIds?: readonly string[];
  readonly internalStructureProtectedCategoryIds?: readonly string[];
  readonly takenChildNamesByCategoryId?: Readonly<Record<string, readonly string[]>>;
  readonly idFactory?: () => string;
  readonly clock?: { nowUtc(): string };
}

// Mirrors the writer's rules — not a convenience stub: the parity suite drives every scenario through
// this mock AND the HTTP adapter in one test, so the mock must RAISE the code the server returns. The
// per-area rules live in the abwab-templates-mock-*-ops.ts modules over one shared state object,
// following the `029` core-mock split; this class is only the port surface and the read projections.
export class AbwabTemplatesMock implements AbwabTemplatesPort {
  private readonly state: AbwabTemplatesMockState;
  private readonly idFactory: () => string;
  private readonly clock: { nowUtc(): string };
  private nextId = 1;

  constructor(options: AbwabTemplatesMockOptions = {}) {
    this.state = {
      templates: new Map(),
      nodes: new Map(),
      aliases: new Map(),
      targetCategoryIds: new Set(options.targetCategoryIds ?? []),
      protectedCategoryIds: new Set(options.internalStructureProtectedCategoryIds ?? []),
      takenChildNames: options.takenChildNamesByCategoryId ?? {},
      timelineGeneration: 1,
      treeRevision: 1,
    };
    this.idFactory = options.idFactory ?? (() => `template-entity-${this.nextId++}`);
    this.clock = options.clock ?? { nowUtc: () => new Date().toISOString() };
  }

  async getTemplates(includeDeleted = false): Promise<DoorTemplateListDto> {
    return {
      schemaVersion: 1,
      templates: [...this.state.templates.values()]
        .filter((template) => includeDeleted || !template.isDeleted)
        .map((template) => this.toSummary(template)),
      expectedTimelineGeneration: { generation: this.state.timelineGeneration },
      serverTimeUtc: this.clock.nowUtc(),
    };
  }

  async getTemplate(doorTemplateId: string, includeDeleted = false): Promise<DoorTemplateDetailDto> {
    const template = requireTemplate(this.state, doorTemplateId);
    const nodes = [...this.state.nodes.values()]
      .filter((node) => node.doorTemplateId === doorTemplateId)
      .filter((node) => includeDeleted || !node.isDeleted)
      .sort((left, right) => left.siblingOrder - right.siblingOrder);

    return {
      schemaVersion: 1,
      template: this.toSummary(template),
      nodes: nodes.map((node) => this.toNodeDto(node, includeDeleted)),
      expectedTimelineGeneration: { generation: this.state.timelineGeneration },
      treeRevision: this.state.treeRevision,
      serverTimeUtc: this.clock.nowUtc(),
    };
  }

  // History entries come from the append-only audit engine, which the mock does not simulate; the
  // envelope is still exercised so the two ports agree on its shape.
  async getHistory(doorTemplateId: string): Promise<TemplateHistoryDto> {
    return {
      schemaVersion: 1,
      doorTemplateId,
      entries: [],
      hasMore: false,
      expectedTimelineGeneration: { generation: this.state.timelineGeneration },
      serverTimeUtc: this.clock.nowUtc(),
    };
  }

  async addTemplate(request: AddDoorTemplateRequest): Promise<AddDoorTemplateResult> {
    return { doorTemplateId: aggregateOps.addTemplate(this.state, this.idFactory, request) };
  }

  async editTemplate(doorTemplateId: string, request: EditDoorTemplateRequest): Promise<void> {
    aggregateOps.editTemplate(this.state, doorTemplateId, request);
  }

  async deleteTemplate(doorTemplateId: string, request: DeleteDoorTemplateRequest): Promise<void> {
    aggregateOps.deleteTemplate(this.state, doorTemplateId, request);
  }

  async restoreTemplate(doorTemplateId: string, request: RestoreDoorTemplateRequest): Promise<void> {
    aggregateOps.restoreTemplate(this.state, doorTemplateId, request);
  }

  async addNode(doorTemplateId: string, request: AddTemplateNodeRequest): Promise<AddTemplateNodeResult> {
    return { templateNodeId: nodeOps.addNode(this.state, this.idFactory, doorTemplateId, request) };
  }

  async editNode(templateNodeId: string, request: EditTemplateNodeRequest): Promise<void> {
    nodeOps.editNode(this.state, templateNodeId, request);
  }

  async reparentNode(templateNodeId: string, request: ReparentTemplateNodeRequest): Promise<void> {
    nodeOps.reparentNode(this.state, templateNodeId, request);
  }

  async reorderNodes(doorTemplateId: string, request: ReorderTemplateNodesRequest): Promise<void> {
    nodeOps.reorderNodes(this.state, doorTemplateId, request);
  }

  async removeNode(templateNodeId: string, request: RemoveTemplateNodeRequest): Promise<void> {
    nodeOps.removeNode(this.state, templateNodeId, request);
  }

  async addNodeAlias(templateNodeId: string, request: AddTemplateNodeAliasRequest): Promise<AddTemplateNodeAliasResult> {
    return { templateNodeSearchAliasId: aliasOps.addNodeAlias(this.state, this.idFactory, templateNodeId, request) };
  }

  async editNodeAlias(aliasId: string, request: EditTemplateNodeAliasRequest): Promise<void> {
    aliasOps.editNodeAlias(this.state, aliasId, request);
  }

  async removeNodeAlias(aliasId: string, request: RemoveTemplateNodeAliasRequest): Promise<void> {
    aliasOps.removeNodeAlias(this.state, aliasId, request);
  }

  async restoreNodeAlias(aliasId: string, request: RestoreTemplateNodeAliasRequest): Promise<void> {
    aliasOps.restoreNodeAlias(this.state, aliasId, request);
  }

  async applyTemplate(doorTemplateId: string, request: ApplyDoorTemplateRequest): Promise<void> {
    applyOps.applyTemplate(this.state, doorTemplateId, request);
  }

  private toSummary(template: MockTemplateRow): DoorTemplateSummaryDto {
    return {
      doorTemplateId: template.doorTemplateId,
      name: template.name,
      description: template.description,
      templateRevision: template.templateRevision,
      isDeleted: template.isDeleted,
      version: template.version,
      nodeCount: [...this.state.nodes.values()].filter(
        (node) => node.doorTemplateId === template.doorTemplateId && !node.isDeleted,
      ).length,
    };
  }

  private toNodeDto(node: MockTemplateNodeRow, includeDeleted: boolean): TemplateNodeDto {
    const aliases: TemplateNodeAliasDto[] = [...this.state.aliases.values()]
      .filter((alias) => alias.templateNodeId === node.templateNodeId)
      .filter((alias) => includeDeleted || !alias.isDeleted)
      .map((alias) => ({
        templateNodeSearchAliasId: alias.templateNodeSearchAliasId,
        value: alias.value,
        isDeleted: alias.isDeleted,
        version: alias.version,
      }));

    return {
      templateNodeId: node.templateNodeId,
      parentTemplateNodeId: node.parentTemplateNodeId,
      name: node.name,
      representativeQuranExcerpt: node.representativeQuranExcerpt,
      description: node.description,
      siblingOrder: node.siblingOrder,
      isDeleted: node.isDeleted,
      version: node.version,
      aliases,
    };
  }
}
