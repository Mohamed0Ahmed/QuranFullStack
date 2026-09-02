import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { LinkingExecutionApi } from '../data-access/linking-execution.api';
import { LinkingJobStatusApi } from '../data-access/linking-job-status.api';
import { LinkingPreparedPreflightApi } from '../data-access/linking-prepared-preflight.api';
import {
  LINKING_RECOVERY_LEASE_MS,
  LINKING_RECOVERY_LEASE_RENEW_MS,
  LINKING_RECOVERY_MAX_BYTES,
  LINKING_RECOVERY_MAX_RECORDS,
  LINKING_TERMINAL_RETENTION_MS,
} from '../linking.policy';
import { isConfirmationJobTerminal } from '../models/linking-execution.models';
import { LinkingPreparedPreflightRequest } from '../models/linking-prepared-preflight.models';
import { LinkingAccessService } from './linking-access.service';

type LinkingRecoveryState = 'open' | 'terminal-unacknowledged';

interface LinkingRecoveryBase {
  id: string;
  actorSub: string;
  state: LinkingRecoveryState;
  updatedAtUtc: string;
  terminalAtUtc: string | null;
}

export interface LinkingPreparationRecoveryReceipt extends LinkingRecoveryBase {
  kind: 'preparation';
  preparationKey: string;
  request?: LinkingPreparedPreflightRequest;
  preflightId?: string;
}

export interface LinkingConfirmationRecoveryReceipt extends LinkingRecoveryBase {
  kind: 'confirmation';
  idempotencyKey: string;
  preflightId: string;
  preflightToken?: string;
  jobRequestHash?: string;
  jobId?: string;
}

export type LinkingRecoveryReceipt =
  | LinkingPreparationRecoveryReceipt
  | LinkingConfirmationRecoveryReceipt;

interface LinkingRecoveryLease {
  actorSub: string;
  ownerId: string;
  expiresAt: number;
}

export class LinkingRecoveryCapacityError extends Error {}

@Injectable({ providedIn: 'root' })
export class LinkingRecoveryStore {
  private readonly authSession = inject(AuthSessionStore);
  private readonly access = inject(LinkingAccessService);
  private readonly preflights = inject(LinkingPreparedPreflightApi);
  private readonly executions = inject(LinkingExecutionApi);
  private readonly jobs = inject(LinkingJobStatusApi);
  private readonly ownerId = crypto.randomUUID();
  private readonly recordsSignal = signal<readonly LinkingRecoveryReceipt[]>([]);
  private readonly recoveringSignal = signal(false);
  private readonly queuedActors = new Set<string>();
  private recoveryQueue: Promise<void> = Promise.resolve();
  private channel: BroadcastChannel | null = null;

  readonly records = this.recordsSignal.asReadonly();
  readonly recovering = this.recoveringSignal.asReadonly();
  readonly hasPending = computed(() => this.recordsSignal().some((record) => record.state === 'open'));

  constructor() {
    if (typeof BroadcastChannel !== 'undefined') {
      this.channel = new BroadcastChannel('quran-dashboard-linking-recovery');
      this.channel.onmessage = ({ data }) => {
        if (data === this.authSession.subject()) {
          void this.refresh(data);
        }
      };
    }
    effect(() => {
      const actorSub = this.access.canUseLinking()
        ? this.authSession.subject()
        : null;
      if (actorSub === null) {
        this.recordsSignal.set([]);
        return;
      }
      void this.refresh(actorSub);
      this.recover(actorSub);
    });
  }

  async appendPreparation(
    actorSub: string,
    request: LinkingPreparedPreflightRequest,
  ): Promise<LinkingPreparationRecoveryReceipt> {
    const preparationKey = requireKey(request.preparationKey, 'مفتاح التحضير مطلوب.');
    const id = receiptId(actorSub, 'preparation', preparationKey);
    const existing = await this.get(id);
    if (existing !== null) {
      if (
        existing.kind !== 'preparation' ||
        existing.preflightId === undefined && canonicalJson(existing.request) !== canonicalJson(request)
      ) {
        throw new Error('مفتاح التحضير مرتبط بطلب مختلف.');
      }
      return existing;
    }
    const receipt: LinkingPreparationRecoveryReceipt = {
      id,
      actorSub,
      kind: 'preparation',
      preparationKey,
      request: structuredClone(request),
      state: 'open',
      updatedAtUtc: new Date().toISOString(),
      terminalAtUtc: null,
    };
    await this.putBounded(receipt);
    await this.afterMutation(actorSub);
    return receipt;
  }

  async setPreflightId(actorSub: string, preparationKey: string, preflightId: string): Promise<void> {
    const existing = await this.requireReceipt(
      receiptId(actorSub, 'preparation', preparationKey),
      'preparation',
    );
    await this.put({
      ...existing,
      request: undefined,
      preflightId,
      updatedAtUtc: new Date().toISOString(),
    });
    await this.afterMutation(actorSub);
  }

  async appendConfirmation(
    actorSub: string,
    preparationKey: string,
    preflightId: string,
    preflightToken: string,
    idempotencyKey: string,
  ): Promise<LinkingConfirmationRecoveryReceipt> {
    const id = receiptId(actorSub, 'confirmation', idempotencyKey);
    const existing = await this.get(id);
    const jobRequestHash = await sha256(canonicalJson({ preflightId, preflightToken }));
    if (existing !== null) {
      if (
        existing.kind !== 'confirmation' ||
        existing.jobId === undefined && existing.jobRequestHash !== jobRequestHash
      ) {
        throw new Error('مفتاح تنفيذ الربط مرتبط بطلب مختلف.');
      }
      return existing;
    }
    const receipt: LinkingConfirmationRecoveryReceipt = {
      id,
      actorSub,
      kind: 'confirmation',
      idempotencyKey,
      preflightId,
      preflightToken,
      jobRequestHash,
      state: 'open',
      updatedAtUtc: new Date().toISOString(),
      terminalAtUtc: null,
    };
    await this.putBounded(receipt);
    await this.delete(receiptId(actorSub, 'preparation', preparationKey));
    await this.afterMutation(actorSub);
    return receipt;
  }

  async setJobId(actorSub: string, idempotencyKey: string, jobId: string): Promise<void> {
    const existing = await this.requireReceipt(
      receiptId(actorSub, 'confirmation', idempotencyKey),
      'confirmation',
    );
    await this.put({
      ...existing,
      preflightToken: undefined,
      jobRequestHash: undefined,
      jobId,
      updatedAtUtc: new Date().toISOString(),
    });
    await this.afterMutation(actorSub);
  }

  async markTerminal(actorSub: string, kind: LinkingRecoveryReceipt['kind'], key: string): Promise<void> {
    const existing = await this.get(receiptId(actorSub, kind, key));
    if (existing === null) {
      return;
    }
    const now = new Date().toISOString();
    await this.put({ ...existing, state: 'terminal-unacknowledged', terminalAtUtc: now, updatedAtUtc: now });
    await this.afterMutation(actorSub);
  }

  async acknowledge(actorSub: string, kind: LinkingRecoveryReceipt['kind'], key: string): Promise<void> {
    await this.delete(receiptId(actorSub, kind, key));
    await this.afterMutation(actorSub);
  }

  recover(actorSub: string): void {
    if (!this.access.canUseLinking() || this.authSession.subject() !== actorSub) {
      return;
    }
    if (this.queuedActors.has(actorSub)) {
      return;
    }
    this.queuedActors.add(actorSub);
    this.recoveryQueue = this.recoveryQueue
      .catch(() => undefined)
      .then(() => this.recoverAsLeader(actorSub))
      .catch(() => undefined)
      .finally(() => this.queuedActors.delete(actorSub));
  }

  private async recoverAsLeader(actorSub: string): Promise<void> {
    if (!(await this.acquireLease(actorSub))) {
      return;
    }
    this.recoveringSignal.set(true);
    const renew = setInterval(() => void this.renewLease(actorSub), LINKING_RECOVERY_LEASE_RENEW_MS);
    try {
      await this.expireOldTerminal(actorSub);
      const records = await this.list(actorSub);
      for (const record of records.filter((candidate) => candidate.state === 'open')) {
        await this.reconcile(record);
      }
      await this.refresh(actorSub);
    } finally {
      clearInterval(renew);
      await this.releaseLease(actorSub);
      this.recoveringSignal.set(false);
    }
  }

  private async reconcile(record: LinkingRecoveryReceipt): Promise<void> {
    try {
      if (record.kind === 'preparation') {
        await this.reconcilePreparation(record);
      } else {
        await this.reconcileConfirmation(record);
      }
    } catch {
      return;
    }
  }

  private async reconcilePreparation(record: LinkingPreparationRecoveryReceipt): Promise<void> {
    let preflightId = record.preflightId;
    if (preflightId === undefined && record.request !== undefined) {
      const status = await firstValueFrom(this.preflights.create(record.request));
      preflightId = status.preflightId;
      await this.setPreflightId(record.actorSub, record.preparationKey, preflightId);
      if (isPreparationReceiptTerminal(status.status)) {
        await this.markTerminal(record.actorSub, 'preparation', record.preparationKey);
      }
      return;
    }
    if (preflightId !== undefined) {
      const status = await firstValueFrom(this.preflights.get(preflightId));
      if (isPreparationReceiptTerminal(status.status)) {
        await this.markTerminal(record.actorSub, 'preparation', record.preparationKey);
      }
    }
  }

  private async reconcileConfirmation(record: LinkingConfirmationRecoveryReceipt): Promise<void> {
    if (record.jobId === undefined && record.preflightToken !== undefined) {
      const submission = await firstValueFrom(
        this.executions.createJob(record.preflightId, record.preflightToken, record.idempotencyKey),
      );
      if (submission.durableOutcome !== null) {
        await this.markTerminal(record.actorSub, 'confirmation', record.idempotencyKey);
      } else if (submission.job !== null) {
        await this.setJobId(record.actorSub, record.idempotencyKey, submission.job.jobId);
        if (isConfirmationJobTerminal(submission.job)) {
          await this.markTerminal(record.actorSub, 'confirmation', record.idempotencyKey);
        }
      }
      return;
    }
    if (record.jobId === undefined) {
      return;
    }
    try {
      const status = await firstValueFrom(this.jobs.get(record.jobId));
      if (isConfirmationJobTerminal(status)) {
        await this.markTerminal(record.actorSub, 'confirmation', record.idempotencyKey);
      }
    } catch {
      await firstValueFrom(this.jobs.getOutcome(record.idempotencyKey));
      await this.markTerminal(record.actorSub, 'confirmation', record.idempotencyKey);
    }
  }

  private async putBounded(receipt: LinkingRecoveryReceipt): Promise<void> {
    const db = await openRecoveryDatabase();
    await transactionDone(db, ['receipts'], 'readwrite', (transaction) => {
      const store = transaction.objectStore('receipts');
      return mutateRequest(
        store.index('actorSub').getAll(receipt.actorSub),
        (existing: LinkingRecoveryReceipt[]) => {
          const retained = existing.filter((candidate) => candidate.id !== receipt.id);
          const discardable = retained
            .filter((candidate) => candidate.state === 'terminal-unacknowledged' || candidate.kind === 'preparation')
            .sort((left, right) =>
              Number(right.state === 'terminal-unacknowledged') - Number(left.state === 'terminal-unacknowledged') ||
              left.updatedAtUtc.localeCompare(right.updatedAtUtc),
            );
          while (exceedsRecoveryCapacity(retained, receipt) && discardable.length > 0) {
            const discarded = discardable.shift()!;
            retained.splice(retained.findIndex((candidate) => candidate.id === discarded.id), 1);
            store.delete(discarded.id);
          }
          if (exceedsRecoveryCapacity(retained, receipt)) {
            throw new LinkingRecoveryCapacityError('سجل استعادة الربط ممتلئ؛ أكمل أو ألغِ العمليات السابقة أولاً.');
          }
          store.put(receipt);
        },
      );
    });
  }

  private async expireOldTerminal(actorSub: string): Promise<void> {
    const cutoff = Date.now() - LINKING_TERMINAL_RETENTION_MS;
    for (const record of await this.list(actorSub)) {
      if (record.state === 'terminal-unacknowledged' && record.terminalAtUtc !== null && Date.parse(record.terminalAtUtc) < cutoff) {
        await this.delete(record.id);
      }
    }
  }

  private async list(actorSub: string): Promise<LinkingRecoveryReceipt[]> {
    const db = await openRecoveryDatabase();
    const transaction = db.transaction('receipts', 'readonly');
    const records = (await requestResult(
      transaction.objectStore('receipts').index('actorSub').getAll(actorSub),
    )) as LinkingRecoveryReceipt[];
    await transactionCompletion(transaction);
    return records.sort((left, right) => left.updatedAtUtc.localeCompare(right.updatedAtUtc));
  }

  private async get(id: string): Promise<LinkingRecoveryReceipt | null> {
    const db = await openRecoveryDatabase();
    const transaction = db.transaction('receipts', 'readonly');
    const record = (await requestResult(transaction.objectStore('receipts').get(id))) as LinkingRecoveryReceipt | undefined;
    await transactionCompletion(transaction);
    return record ?? null;
  }

  private async requireReceipt<K extends LinkingRecoveryReceipt['kind']>(
    id: string,
    kind: K,
  ): Promise<Extract<LinkingRecoveryReceipt, { kind: K }>> {
    const record = await this.get(id);
    if (record === null || record.kind !== kind) {
      throw new Error('تعذر العثور على إيصال استعادة الربط.');
    }
    return record as Extract<LinkingRecoveryReceipt, { kind: K }>;
  }

  private async put(receipt: LinkingRecoveryReceipt): Promise<void> {
    const db = await openRecoveryDatabase();
    await transactionDone(db, ['receipts'], 'readwrite', (transaction) => {
      transaction.objectStore('receipts').put(receipt);
    });
  }

  private async delete(id: string): Promise<void> {
    const db = await openRecoveryDatabase();
    await transactionDone(db, ['receipts'], 'readwrite', (transaction) => {
      transaction.objectStore('receipts').delete(id);
    });
  }

  private async refresh(actorSub: string): Promise<void> {
    if (this.authSession.subject() === actorSub) {
      this.recordsSignal.set(await this.list(actorSub));
    }
  }

  private async afterMutation(actorSub: string): Promise<void> {
    await this.refresh(actorSub);
    this.channel?.postMessage(actorSub);
  }

  private async acquireLease(actorSub: string): Promise<boolean> {
    const db = await openRecoveryDatabase();
    let acquired = false;
    await transactionDone(db, ['leases'], 'readwrite', (transaction) => {
      const store = transaction.objectStore('leases');
      return mutateRequest(store.get(actorSub), (existing: LinkingRecoveryLease | undefined) => {
        if (existing === undefined || existing.expiresAt <= Date.now() || existing.ownerId === this.ownerId) {
          store.put({ actorSub, ownerId: this.ownerId, expiresAt: Date.now() + LINKING_RECOVERY_LEASE_MS });
          acquired = true;
        }
      });
    });
    return acquired;
  }

  private async renewLease(actorSub: string): Promise<void> {
    const db = await openRecoveryDatabase();
    await transactionDone(db, ['leases'], 'readwrite', (transaction) => {
      const store = transaction.objectStore('leases');
      return mutateRequest(store.get(actorSub), (existing: LinkingRecoveryLease | undefined) => {
        if (existing?.ownerId === this.ownerId) {
          store.put({ ...existing, expiresAt: Date.now() + LINKING_RECOVERY_LEASE_MS });
        }
      });
    });
  }

  private async releaseLease(actorSub: string): Promise<void> {
    const db = await openRecoveryDatabase();
    await transactionDone(db, ['leases'], 'readwrite', (transaction) => {
      const store = transaction.objectStore('leases');
      return mutateRequest(store.get(actorSub), (existing: LinkingRecoveryLease | undefined) => {
        if (existing?.ownerId === this.ownerId) {
          store.delete(actorSub);
        }
      });
    });
  }
}

let databasePromise: Promise<IDBDatabase> | null = null;

function openRecoveryDatabase(): Promise<IDBDatabase> {
  databasePromise ??= new Promise((resolve, reject) => {
    const request = indexedDB.open('quran-dashboard-linking-recovery', 1);
    request.onupgradeneeded = () => {
      const database = request.result;
      const receipts = database.createObjectStore('receipts', { keyPath: 'id' });
      receipts.createIndex('actorSub', 'actorSub');
      receipts.createIndex('actorState', ['actorSub', 'state']);
      database.createObjectStore('leases', { keyPath: 'actorSub' });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error('تعذر فتح سجل استعادة الربط.'));
  });
  return databasePromise;
}

async function transactionDone(
  database: IDBDatabase,
  stores: string[],
  mode: IDBTransactionMode,
  work: (transaction: IDBTransaction) => void | Promise<void>,
): Promise<void> {
  const transaction = database.transaction(stores, mode);
  const completion = transactionCompletion(transaction);
  try {
    await work(transaction);
    await completion;
  } catch (error: unknown) {
    try {
      transaction.abort();
    } catch {}
    await completion.catch(() => undefined);
    throw error;
  }
}

function transactionCompletion(transaction: IDBTransaction): Promise<void> {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error ?? new Error('تعذر حفظ سجل استعادة الربط.'));
    transaction.onabort = () => reject(transaction.error ?? new Error('أُلغي حفظ سجل استعادة الربط.'));
  });
}

function requestResult<T>(request: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error('تعذر قراءة سجل استعادة الربط.'));
  });
}

function mutateRequest<T>(request: IDBRequest<T>, mutate: (value: T) => void): Promise<void> {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => {
      try {
        mutate(request.result);
        resolve();
      } catch (error: unknown) {
        reject(error);
      }
    };
    request.onerror = () => reject(request.error ?? new Error('تعذر تحديث سجل استعادة الربط.'));
  });
}

function receiptId(actorSub: string, kind: LinkingRecoveryReceipt['kind'], key: string): string {
  return `${actorSub}:${kind}:${key}`;
}

function requireKey(value: string | null, message: string): string {
  if (value === null || value.trim().length === 0) {
    throw new Error(message);
  }
  return value;
}

function serializedBytes(value: unknown): number {
  return new TextEncoder().encode(JSON.stringify(value)).byteLength;
}

function exceedsRecoveryCapacity(
  retained: readonly LinkingRecoveryReceipt[],
  receipt: LinkingRecoveryReceipt,
): boolean {
  const records = [...retained, receipt];
  return records.length > LINKING_RECOVERY_MAX_RECORDS || serializedBytes(records) > LINKING_RECOVERY_MAX_BYTES;
}

function canonicalJson(value: unknown): string {
  if (value === undefined) {
    return '';
  }
  if (Array.isArray(value)) {
    return `[${value.map(canonicalJson).join(',')}]`;
  }
  if (value !== null && typeof value === 'object') {
    return `{${Object.entries(value)
      .filter(([, child]) => child !== undefined)
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([key, child]) => `${JSON.stringify(key)}:${canonicalJson(child)}`)
      .join(',')}}`;
  }
  return JSON.stringify(value);
}

async function sha256(value: string): Promise<string> {
  const bytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(value));
  return [...new Uint8Array(bytes)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}

function isPreparationReceiptTerminal(status: string): boolean {
  return ['failed', 'cancelled', 'expired'].includes(status.toLowerCase());
}
