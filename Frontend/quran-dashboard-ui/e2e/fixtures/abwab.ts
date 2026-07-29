import type { APIRequestContext, Locator, Page } from '@playwright/test';

import { test as base, expect } from './app-test';

export { expect };

// Same host the backend launch profile serves (playwright.config.ts's API_HEALTH_URL) —
// setup/teardown go through the API directly, never the UI, so a flow that breaks the UI
// still tears down cleanly (plan-slice-b2.md T601).
const API_BASE = 'https://localhost:5015';

export interface AbwabSandboxDoor {
  readonly id: number;
  readonly version: number;
  readonly name: string;
  readonly parentId: number | null;
}

export interface AbwabSandboxCreateDoorInput {
  readonly name: string;
  readonly parentId?: number | null;
  readonly aliases?: readonly string[];
}

export interface AbwabSandbox {
  readonly sectionId: number;
  readonly sectionName: string;
  /** `${sectionName}-<suffix>` — unique across both parallel workers and across runs, for
   * any door/alias name a flow searches for or asserts on (R18). */
  uniqueName(suffix: string): string;
  createDoor(input: AbwabSandboxCreateDoorInput): Promise<AbwabSandboxDoor>;
  /**
   * Archives a door directly over the API, for flows that need an already-archived door as
   * a precondition rather than as the thing under test — same reason `createDoor` does not
   * drive the modal. Archive-through-the-UI has its own flow in `abwab-archive.e2e.ts`.
   */
  archiveDoor(doorId: number): Promise<void>;
}

async function createSectionViaApi(request: APIRequestContext, name: string): Promise<number> {
  const res = await request.post(`${API_BASE}/api/abwab/sections`, { data: { name } });
  expect(res.ok(), `abwab sandbox: section create failed (${res.status()})`).toBeTruthy();
  const json = await res.json();
  return json['data']['id'] as number;
}

async function createDoorViaApi(
  request: APIRequestContext,
  sectionId: number,
  input: AbwabSandboxCreateDoorInput,
): Promise<AbwabSandboxDoor> {
  const body: Record<string, unknown> = {
    name: input.name,
    description: null,
    representativeAyahText: null,
    aliases: input.aliases ? [...input.aliases] : [],
    parentId: input.parentId ?? null,
  };
  // The backend derives the section from `parentId` once it is set, and 400s on a stated
  // mismatch (plan-slice-b.md §4 input 5) — so `sectionId` must be genuinely absent, not
  // `null`, once a parent is given (mirrors `AbwabApi#buildCreateDoorBody`).
  if (!input.parentId) {
    body['sectionId'] = sectionId;
  }
  const res = await request.post(`${API_BASE}/api/abwab/doors`, { data: body });
  expect(res.ok(), `abwab sandbox: door create failed (${res.status()})`).toBeTruthy();
  const json = await res.json();
  const door = json['data'];
  return { id: door.id, version: door.version, name: door.name, parentId: door.parentId };
}

interface AbwabTreeDoorRow {
  readonly id: number;
  readonly version: number;
  readonly isArchived: boolean;
  readonly sectionId: number | null;
}

async function fetchTreeDoors(request: APIRequestContext): Promise<Map<number, AbwabTreeDoorRow>> {
  const res = await request.get(`${API_BASE}/api/abwab/tree`);
  const json = await res.json();
  const doors = (json?.['data']?.['doors'] ?? []) as AbwabTreeDoorRow[];
  return new Map(doors.map((door) => [door.id, door]));
}

async function archiveDoorViaApi(request: APIRequestContext, doorId: number): Promise<void> {
  // Re-reads the current version rather than trusting a caller-supplied one — correct even
  // if the door was edited/moved since it was created.
  const doorsById = await fetchTreeDoors(request);
  const door = doorsById.get(doorId);
  if (!door) {
    throw new Error(`abwab sandbox: cannot archive unknown door ${doorId}`);
  }
  const res = await request.delete(`${API_BASE}/api/abwab/doors/${doorId}`, { data: { version: door.version } });
  expect(res.ok(), `abwab sandbox: door archive failed (${res.status()})`).toBeTruthy();
}

/**
 * Best-effort teardown (R19): a flow that already archived, moved, or deleted its own
 * sandbox rows must not turn cleanup into a second failure that masks the real one — so
 * nothing in here throws. Archive-then-delete is the only lawful order (section delete
 * `409`s while it holds live doors, `AbwabSectionsController.cs:71`); R20's detach flow
 * (T604) deletes its own section mid-test, so the delete below 404s there and is swallowed.
 *
 * Two things this has to get right, both learned the hard way from real residue:
 *
 * 1. **Re-read the version before every archive.** Every write resequences its scope to
 *    `1..N`, which bumps every sibling's `xmin` (plan-slice-b.md §4.6/R10). Archiving from
 *    one up-front snapshot therefore succeeds for the first door and `409`s for all the
 *    rest — silently, because a `409` is a response, not a thrown error. One fetch per
 *    archive is the price of the invariant.
 * 2. **Sweep the section, not just the ids the fixture handed out.** Flows create doors
 *    *through the UI*, and those ids were never recorded here. A door the sweep misses stays
 *    live, which then `409`s the section delete, which leaves the section in the tab strip
 *    too. Section membership is inherited from the parent, so scoping by `sectionId` covers
 *    UI-created roots and their children alike.
 */
async function archiveEverythingIn(
  request: APIRequestContext,
  sectionIds: ReadonlySet<number>,
  doorIds: ReadonlySet<number>,
): Promise<void> {
  const isSandboxDoor = (door: AbwabTreeDoorRow): boolean =>
    !door.isArchived && ((door.sectionId !== null && sectionIds.has(door.sectionId)) || doorIds.has(door.id));

  // Bounded rather than `while (true)`: a door that refuses to archive must end the loop,
  // not spin it. The bound is generous against any single test's door count.
  for (let attempt = 0; attempt < 60; attempt += 1) {
    const doorsById = await fetchTreeDoors(request);
    const next = [...doorsById.values()].find(isSandboxDoor);
    if (!next) {
      return;
    }
    const res = await request
      .delete(`${API_BASE}/api/abwab/doors/${next.id}`, { data: { version: next.version } })
      .catch(() => null);
    if (!res || !res.ok()) {
      return; // cannot make progress; leave the rest rather than spin or throw
    }
  }
}

async function sectionExists(request: APIRequestContext, sectionId: number): Promise<boolean> {
  const res = await request.get(`${API_BASE}/api/abwab/tree`);
  const json = await res.json();
  const sections = (json?.['data']?.['sections'] ?? []) as Array<{ id: number }>;
  return sections.some((section) => section.id === sectionId);
}

async function teardownSandbox(
  request: APIRequestContext,
  sectionIds: ReadonlySet<number>,
  doorIds: ReadonlySet<number>,
): Promise<void> {
  for (const sectionId of sectionIds) {
    // Retried, not fire-and-forget: two workers write one database concurrently, and a
    // section delete can lose a race and answer non-2xx even though nothing is wrong with
    // it — a single silent attempt is how a deletable section survived a run. Re-sweep
    // before each attempt so a door created between attempts cannot block the delete.
    for (let attempt = 0; attempt < 3; attempt += 1) {
      try {
        await archiveEverythingIn(request, sectionIds, doorIds);
        const res = await request.delete(`${API_BASE}/api/abwab/sections/${sectionId}`);
        if (res.ok() || res.status() === 404 || !(await sectionExists(request, sectionId))) {
          break; // gone, or the flow deleted it itself (R20's detach case)
        }
      } catch {
        // Best-effort (R19): teardown must never raise a second failure that masks the
        // test's own. Fall through to the next attempt, then give up quietly.
      }
    }
  }
}

export const test = base.extend<{ abwabSandbox: AbwabSandbox }>({
  abwabSandbox: async ({ request }, use, testInfo) => {
    const sectionName = `e2e-sandbox-w${testInfo.workerIndex}-${Date.now()}`;
    const sectionId = await createSectionViaApi(request, sectionName);
    const sectionIds = new Set<number>([sectionId]);
    const doorIds = new Set<number>();

    const sandbox: AbwabSandbox = {
      sectionId,
      sectionName,
      uniqueName: (suffix: string) => `${sectionName}-${suffix}`,
      createDoor: async (input) => {
        const door = await createDoorViaApi(request, sectionId, input);
        doorIds.add(door.id);
        return door;
      },
      archiveDoor: (doorId) => archiveDoorViaApi(request, doorId),
    };

    await use(sandbox);

    await teardownSandbox(request, sectionIds, doorIds);
  },
});

/** Locates a tree row by its visible name — for doors created through the UI itself,
 * where the id isn't known ahead of time. A door created via `abwabSandbox.createDoor`
 * should be addressed directly by its returned id (`abwab-tree-row-<id>`) instead. */
export function abwabTreeRowByName(page: Page, name: string): Locator {
  return page.locator('[data-testid^="abwab-tree-row-"]').filter({ hasText: name });
}

/** Parses the numeric id suffix off a `data-testid` attribute, e.g. `abwab-tree-row-42`. */
export async function abwabTestIdNumber(locator: Locator): Promise<number> {
  const testId = await locator.getAttribute('data-testid');
  const match = testId?.match(/-(\d+)$/);
  if (!match) {
    throw new Error(`abwab e2e: could not parse a door id out of testid "${testId}"`);
  }
  return Number(match[1]);
}
