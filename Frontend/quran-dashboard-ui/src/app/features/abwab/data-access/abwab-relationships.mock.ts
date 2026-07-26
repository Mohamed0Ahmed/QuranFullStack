import {
  AddRelationshipRequest,
  CategoryRelationshipDto,
  CategoryRelationshipEndpointDto,
  CategoryRelationshipListDto,
  DeleteRelationshipRequest,
  EditRelationshipRequest,
  RelationshipType,
  RestoreRelationshipRequest,
} from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import {
  AbwabRelationshipsPort,
  AddRelationshipResult,
  RELATIONSHIP_TYPE_BROADER_NARROWER,
} from './abwab-relationships.port';

export interface AbwabRelationshipsMockOptions {
  readonly categoryIds?: readonly string[];
  readonly deletedCategoryIds?: readonly string[];
  readonly relationshipProtectedCategoryIds?: readonly string[];
  readonly idFactory?: () => string;
  readonly clock?: { nowUtc(): string };
}

interface MockRelationshipRow {
  categoryRelationshipId: string;
  relationshipType: RelationshipType;
  lowerCategoryId: string | null;
  higherCategoryId: string | null;
  sourceCategoryId: string | null;
  targetCategoryId: string | null;
  isDeleted: boolean;
  version: number;
}

// Mirrors CategoryRelationship.Canonicalize (Guid.CompareTo). .NET 10 compares Guid fields
// UNSIGNED in the order _a, _b, _c, _d.._k, and the canonical "D" string renders exactly those
// fields as fixed-width big-endian hex in that order — so lower-cased lexicographic comparison is
// equivalent. The explicit lower-casing is load-bearing: raw code-unit order would sort an
// upper-cased id before a lower-cased one and desync the mock from the server's canonical pair.
function canonicalizeEndpointPair(first: string, second: string): [string, string] {
  return first.toLowerCase() < second.toLowerCase() ? [first, second] : [second, first];
}

export class AbwabRelationshipsMock implements AbwabRelationshipsPort {
  private readonly rows = new Map<string, MockRelationshipRow>();
  private readonly categoryNames = new Map<string, string>();
  private readonly deletedCategoryIds: Set<string>;
  private readonly protectedCategoryIds: Set<string>;
  private readonly idFactory: () => string;
  private readonly clock: { nowUtc(): string };
  private timelineGeneration = 1;
  private nextId = 1;

  constructor(options: AbwabRelationshipsMockOptions = {}) {
    for (const categoryId of options.categoryIds ?? []) {
      this.categoryNames.set(categoryId, `باب ${categoryId}`);
    }
    this.deletedCategoryIds = new Set(options.deletedCategoryIds ?? []);
    this.protectedCategoryIds = new Set(options.relationshipProtectedCategoryIds ?? []);
    this.idFactory = options.idFactory ?? (() => `relationship-${this.nextId++}`);
    this.clock = options.clock ?? { nowUtc: () => new Date().toISOString() };
  }

  setCategoryDeleted(categoryId: string, isDeleted: boolean): void {
    if (isDeleted) {
      this.deletedCategoryIds.add(categoryId);
    } else {
      this.deletedCategoryIds.delete(categoryId);
    }
  }

  async getRelationships(categoryId: string, includeDeleted = false): Promise<CategoryRelationshipListDto> {
    const relationships = [...this.rows.values()]
      .filter((row) => this.endpointsOf(row).includes(categoryId))
      .filter((row) => includeDeleted || !row.isDeleted)
      .filter((row) => this.endpointsOf(row).every((id) => !this.deletedCategoryIds.has(id)))
      .map((row) => this.toDto(row));

    return {
      schemaVersion: 1,
      categoryId,
      expectedTimelineGeneration: { generation: this.timelineGeneration },
      serverTimeUtc: this.clock.nowUtc(),
      relationships,
    };
  }

  async addRelationship(request: AddRelationshipRequest): Promise<AddRelationshipResult> {
    this.assertGeneration(request.expectedTimelineGeneration);
    const shape = this.toShape(request.relationshipType, request.firstCategoryId, request.secondCategoryId);

    this.assertTargetsWritable(this.endpointsOf(shape));
    this.assertNoDuplicate(shape, null);
    this.assertNoCycle(shape, null);

    const categoryRelationshipId = this.idFactory();
    this.rows.set(categoryRelationshipId, { ...shape, categoryRelationshipId, isDeleted: false, version: 1 });
    return { relationshipId: categoryRelationshipId };
  }

  async editRelationship(relationshipId: string, request: EditRelationshipRequest): Promise<void> {
    this.assertGeneration(request.expectedTimelineGeneration);
    const row = this.requireRow(relationshipId, request.expectedVersion);
    const proposed = this.toShape(request.relationshipType, request.firstCategoryId, request.secondCategoryId);

    this.assertTargetsWritable([...this.endpointsOf(row), ...this.endpointsOf(proposed)]);
    this.assertState(row, { isDeleted: false });
    this.assertNoDuplicate(proposed, relationshipId);
    this.assertNoCycle(proposed, relationshipId);

    this.rows.set(relationshipId, { ...row, ...proposed, version: row.version + 1 });
  }

  async deleteRelationship(relationshipId: string, request: DeleteRelationshipRequest): Promise<void> {
    this.assertGeneration(request.expectedTimelineGeneration);
    const row = this.requireRow(relationshipId, request.expectedVersion);

    this.assertTargetsWritable(this.endpointsOf(row));
    this.assertState(row, { isDeleted: false });
    this.rows.set(relationshipId, { ...row, isDeleted: true, version: row.version + 1 });
  }

  async restoreRelationship(relationshipId: string, request: RestoreRelationshipRequest): Promise<void> {
    this.assertGeneration(request.expectedTimelineGeneration);
    const row = this.requireRow(relationshipId, request.expectedVersion);

    this.assertTargetsWritable(this.endpointsOf(row));
    this.assertState(row, { isDeleted: true });
    this.assertNoDuplicate(row, relationshipId);
    this.assertNoCycle(row, relationshipId);

    this.rows.set(relationshipId, { ...row, isDeleted: false, version: row.version + 1 });
  }

  private toShape(
    relationshipType: RelationshipType,
    firstCategoryId: string,
    secondCategoryId: string,
  ): Omit<MockRelationshipRow, 'categoryRelationshipId' | 'isDeleted' | 'version'> {
    if (firstCategoryId === secondCategoryId) {
      throw new Error('A relationship cannot link a category to itself.');
    }

    if (relationshipType === RELATIONSHIP_TYPE_BROADER_NARROWER) {
      return {
        relationshipType,
        lowerCategoryId: null,
        higherCategoryId: null,
        sourceCategoryId: firstCategoryId,
        targetCategoryId: secondCategoryId,
      };
    }

    const [lower, higher] = canonicalizeEndpointPair(firstCategoryId, secondCategoryId);
    return { relationshipType, lowerCategoryId: lower, higherCategoryId: higher, sourceCategoryId: null, targetCategoryId: null };
  }

  private endpointsOf(row: Pick<MockRelationshipRow, 'lowerCategoryId' | 'higherCategoryId' | 'sourceCategoryId' | 'targetCategoryId'>): string[] {
    return [row.lowerCategoryId, row.higherCategoryId, row.sourceCategoryId, row.targetCategoryId].filter(
      (id): id is string => id !== null,
    );
  }

  private requireRow(relationshipId: string, expectedVersion: number): MockRelationshipRow {
    const row = this.rows.get(relationshipId);
    if (!row || row.version !== expectedVersion) {
      throw new AbwabConflictError('abwab.row_stale');
    }
    return row;
  }

  // Mirrors the writer: a row already in the requested state is refused as stale rather than
  // answered as done, and the protection gate runs BEFORE this check on both sides.
  private assertState(row: MockRelationshipRow, expected: { isDeleted: boolean }): void {
    if (row.isDeleted !== expected.isDeleted) {
      throw new AbwabConflictError('abwab.row_stale');
    }
  }

  private assertGeneration(expected: number): void {
    if (expected !== this.timelineGeneration) {
      throw new AbwabConflictError('abwab.timeline_generation_stale');
    }
  }

  private assertTargetsWritable(endpointCategoryIds: readonly string[]): void {
    for (const categoryId of endpointCategoryIds) {
      if (this.protectedCategoryIds.has(categoryId)) {
        throw new AbwabConflictError('abwab.manual_protection');
      }
    }
    for (const categoryId of endpointCategoryIds) {
      if (this.deletedCategoryIds.has(categoryId)) {
        throw new AbwabConflictError('abwab.category_unavailable');
      }
    }
  }

  private assertNoDuplicate(
    shape: Pick<MockRelationshipRow, 'relationshipType' | 'lowerCategoryId' | 'higherCategoryId' | 'sourceCategoryId' | 'targetCategoryId'>,
    excludeRelationshipId: string | null,
  ): void {
    const duplicate = [...this.rows.values()].some(
      (row) =>
        !row.isDeleted &&
        row.categoryRelationshipId !== excludeRelationshipId &&
        row.relationshipType === shape.relationshipType &&
        row.lowerCategoryId === shape.lowerCategoryId &&
        row.higherCategoryId === shape.higherCategoryId &&
        row.sourceCategoryId === shape.sourceCategoryId &&
        row.targetCategoryId === shape.targetCategoryId,
    );

    if (duplicate) {
      throw new AbwabConflictError('abwab.relationship_duplicate');
    }
  }

  private assertNoCycle(
    shape: Pick<MockRelationshipRow, 'relationshipType' | 'sourceCategoryId' | 'targetCategoryId'>,
    excludeRelationshipId: string | null,
  ): void {
    if (shape.relationshipType !== RELATIONSHIP_TYPE_BROADER_NARROWER || !shape.sourceCategoryId || !shape.targetCategoryId) {
      return;
    }

    const broader = shape.sourceCategoryId;
    const visited = new Set<string>([shape.targetCategoryId]);
    let frontier = [shape.targetCategoryId];

    while (frontier.length > 0) {
      const reached = [...this.rows.values()]
        .filter(
          (row) =>
            !row.isDeleted &&
            row.categoryRelationshipId !== excludeRelationshipId &&
            row.sourceCategoryId !== null &&
            frontier.includes(row.sourceCategoryId),
        )
        .map((row) => row.targetCategoryId)
        .filter((id): id is string => id !== null);

      if (reached.includes(broader)) {
        throw new AbwabConflictError('abwab.relationship_cycle');
      }

      frontier = reached.filter((id) => !visited.has(id));
      frontier.forEach((id) => visited.add(id));
    }
  }

  private toDto(row: MockRelationshipRow): CategoryRelationshipDto {
    const [from, to] = this.endpointsOf(row);
    return {
      categoryRelationshipId: row.categoryRelationshipId,
      relationshipType: row.relationshipType,
      isDirectional: row.relationshipType === RELATIONSHIP_TYPE_BROADER_NARROWER,
      from: this.toEndpointDto(from),
      to: this.toEndpointDto(to),
      isDeleted: row.isDeleted,
      version: row.version,
    };
  }

  private toEndpointDto(categoryId: string): CategoryRelationshipEndpointDto {
    return {
      categoryId,
      name: this.categoryNames.get(categoryId) ?? categoryId,
      path: [this.categoryNames.get(categoryId) ?? categoryId],
      isDeleted: this.deletedCategoryIds.has(categoryId),
    };
  }
}
