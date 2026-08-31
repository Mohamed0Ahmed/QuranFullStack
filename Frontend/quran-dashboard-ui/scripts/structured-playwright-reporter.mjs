import { mkdirSync, writeFileSync } from 'node:fs';
import { basename, relative, resolve } from 'node:path';

import {
  redactDiagnosticText,
  sensitiveEnvironmentValues,
} from '../e2e/harness/sealed-execution-contract.mjs';

export default class StructuredPlaywrightReporter {
  constructor() {
    const configured = process.env.E2E_EVIDENCE_DIRECTORY?.trim();
    if (!configured) {
      throw new Error('Structured Playwright reporting requires E2E_EVIDENCE_DIRECTORY.');
    }
    this.evidenceDirectory = resolve(configured);
    this.secretValues = sensitiveEnvironmentValues(process.env);
    this.applicationsReadyAt = undefined;
    this.tests = [];
    this.stepStarts = new WeakMap();
    this.traceEvents = [];
    mkdirSync(this.evidenceDirectory, { recursive: true });
  }

  onBegin(_config, suite) {
    this.applicationsReadyAt = new Date().toISOString();
    this.declaredTestCount = suite.allTests().length;
  }

  onTestEnd(test, result) {
    const approved = result.attachments.filter((attachment) =>
      APPROVED_ATTACHMENTS.has(attachment.name),
    );
    const approvedNames = approved.map((attachment) => attachment.name);
    if (new Set(approvedNames).size !== approvedNames.length) {
      throw new Error('Each approved diagnostic attachment may appear at most once per test result.');
    }
    const attachments = approved.map((attachment) => this.persistAttachment(test, attachment));
    this.tests.push({
      id: test.id,
      title: test.titlePath().join(' > '),
      file: test.location.file,
      line: test.location.line,
      status: result.status,
      durationMs: result.duration,
      retry: result.retry,
      errors: result.errors.map((error) =>
        redactDiagnosticText(error.message ?? error.value ?? 'Unknown Playwright error.', this.secretValues),
      ),
      attachments,
    });
  }

  persistAttachment(test, attachment) {
    const policy = APPROVED_ATTACHMENTS.get(attachment.name);
    if (attachment.contentType !== policy.contentType) {
      throw new Error(
        `Approved diagnostic attachment ${attachment.name} must use ${policy.contentType}.`,
      );
    }
    if (attachment.path || !Buffer.isBuffer(attachment.body)) {
      throw new Error(
        `Approved diagnostic attachment ${attachment.name} must provide an in-memory body.`,
      );
    }
    if (attachment.body.length === 0 || attachment.body.length > policy.maxBytes) {
      throw new Error(`Approved diagnostic attachment ${attachment.name} has an unsafe size.`);
    }
    const directory = resolve(
      this.evidenceDirectory,
      'diagnostics',
      test.id.replaceAll(/[^A-Za-z0-9._-]/g, '-'),
    );
    mkdirSync(directory, { recursive: true });
    const path = resolve(directory, policy.file);
    const content = sanitizeAndValidateDiagnostic(
      attachment.name,
      attachment.body,
      this.secretValues,
    );
    writeFileSync(path, content, { mode: 0o600 });
    return {
      name: attachment.name,
      contentType: policy.contentType,
      file: relative(this.evidenceDirectory, path),
    };
  }

  onStepBegin(test, _result, step) {
    this.stepStarts.set(step, {
      testId: test.id,
      category: step.category,
      title: redactDiagnosticText(step.title, this.secretValues),
      startedAt: step.startTime.toISOString(),
      location: step.location
        ? { file: basename(step.location.file), line: step.location.line }
        : undefined,
    });
  }

  onStepEnd(_test, _result, step) {
    const event = this.stepStarts.get(step);
    if (!event) return;
    this.traceEvents.push({
      ...event,
      durationMs: step.duration,
      error: step.error
        ? redactDiagnosticText(step.error.message ?? step.error.value ?? 'Step failed.', this.secretValues)
        : undefined,
    });
  }

  onEnd(result) {
    const completedAt = new Date().toISOString();
    const applicationsReadyAt = this.applicationsReadyAt ?? completedAt;
    const output = {
      schemaVersion: 1,
      status: result.status,
      applicationsReadyAt,
      completedAt,
      durationMs: result.duration,
      declaredTestCount: this.declaredTestCount ?? 0,
      counts: countStatuses(this.tests),
      tests: this.tests,
    };
    mkdirSync(this.evidenceDirectory, { recursive: true });
    writeFileSync(
      resolve(this.evidenceDirectory, 'playwright-results.json'),
      `${JSON.stringify(output, null, 2)}\n`,
      { encoding: 'utf8', mode: 0o600 },
    );
    if (result.status !== 'passed') {
      const failedTestIds = new Set(
        this.tests.filter((test) => test.status !== 'passed').map((test) => test.id),
      );
      writeFileSync(
        resolve(this.evidenceDirectory, 'sanitized-trace.json'),
        `${JSON.stringify({
          schemaVersion: 1,
          status: result.status,
          containsNetworkHeaders: false,
          containsRequestBodies: false,
          containsResponseBodies: false,
          events: this.traceEvents.filter((event) => failedTestIds.has(event.testId)),
        }, null, 2)}\n`,
        { encoding: 'utf8', mode: 0o600 },
      );
    }
  }
}

const APPROVED_ATTACHMENTS = new Map([
  ['browser-console-errors', {
    contentType: 'application/json',
    file: 'browser-console-errors.json',
    maxBytes: 1024 * 1024,
  }],
  ['request-metadata', {
    contentType: 'application/json',
    file: 'request-metadata.json',
    maxBytes: 1024 * 1024,
  }],
  ['sanitized-screenshot', {
    contentType: 'image/png',
    file: 'sanitized-screenshot.png',
    maxBytes: 20 * 1024 * 1024,
  }],
]);

const PNG_SIGNATURE = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

export const APPROVED_DIAGNOSTIC_FILES = Object.freeze(
  [...APPROVED_ATTACHMENTS.values()].map((policy) => policy.file),
);

export function validateRetainedDiagnostic(file, content) {
  const entry = [...APPROVED_ATTACHMENTS.entries()].find(([, policy]) => policy.file === file);
  if (!entry) throw new Error(`Unexpected retained diagnostic file ${file}.`);
  const [name, policy] = entry;
  if (!Buffer.isBuffer(content) || content.length === 0 || content.length > policy.maxBytes) {
    throw new Error(`Retained diagnostic ${file} has an unsafe size.`);
  }
  validateDiagnosticBody(name, content);
}

function sanitizeAndValidateDiagnostic(name, body, secretValues) {
  if (name === 'sanitized-screenshot') {
    validateDiagnosticBody(name, body);
    return body;
  }
  const sanitized = Buffer.from(redactDiagnosticText(body.toString('utf8'), secretValues));
  const parsed = validateDiagnosticBody(name, sanitized);
  return Buffer.from(`${JSON.stringify(parsed, null, 2)}\n`);
}

function validateDiagnosticBody(name, body) {
  if (name === 'sanitized-screenshot') {
    if (body.length < PNG_SIGNATURE.length || !body.subarray(0, PNG_SIGNATURE.length).equals(PNG_SIGNATURE)) {
      throw new Error('The sanitized screenshot must have a valid PNG signature.');
    }
    return undefined;
  }
  let parsed;
  try {
    parsed = JSON.parse(body.toString('utf8'));
  } catch {
    throw new Error(`Approved diagnostic attachment ${name} must contain valid JSON.`);
  }
  if (!Array.isArray(parsed)) {
    throw new Error(`Approved diagnostic attachment ${name} must contain a JSON array.`);
  }
  if (name === 'request-metadata') validateRequestMetadata(parsed);
  else validateConsoleErrors(parsed);
  return parsed;
}

function validateRequestMetadata(entries) {
  if (entries.length > 1000) throw new Error('Request metadata exceeds the retention limit.');
  const keysByEvent = new Map([
    ['request', new Set(['event', 'method', 'origin', 'path', 'resourceType'])],
    ['response', new Set(['event', 'method', 'origin', 'path', 'status'])],
    ['requestfailed', new Set(['event', 'method', 'origin', 'path', 'error'])],
  ]);
  for (const entry of entries) {
    requireRecord(entry, 'request metadata entry');
    const allowedKeys = keysByEvent.get(entry.event);
    if (!allowedKeys) throw new Error('Request metadata contains an unknown event.');
    requireExactKeys(entry, allowedKeys, 'request metadata entry');
    requireShortString(entry.method, 'request method', 32);
    requireShortString(entry.origin, 'request origin', 2048);
    requireShortString(entry.path, 'request path', 4096);
    const origin = new URL(entry.origin);
    if (!['http:', 'https:'].includes(origin.protocol) || origin.origin !== entry.origin) {
      throw new Error('Request metadata origin must contain only an HTTP(S) origin.');
    }
    if (!entry.path.startsWith('/') || /[?#]/.test(entry.path)) {
      throw new Error('Request metadata path must not contain a query or fragment.');
    }
    if (entry.event === 'request') requireShortString(entry.resourceType, 'resource type', 64);
    if (entry.event === 'response' && (!Number.isInteger(entry.status) || entry.status < 100 || entry.status > 599)) {
      throw new Error('Response metadata status must be a valid HTTP status.');
    }
    if (entry.event === 'requestfailed') requireShortString(entry.error, 'request error', 4000);
  }
}

function validateConsoleErrors(entries) {
  if (entries.length > 250) throw new Error('Browser console diagnostics exceed the retention limit.');
  for (const entry of entries) {
    requireRecord(entry, 'browser console entry');
    if (entry.type === 'error') {
      requireExactKeys(
        entry,
        new Set(['type', 'text', 'location', 'line', 'column']),
        'browser console entry',
      );
      requireShortString(entry.location, 'browser console location', 4096, true);
      if (!Number.isInteger(entry.line) || !Number.isInteger(entry.column)) {
        throw new Error('Browser console line and column must be integers.');
      }
    } else if (entry.type === 'pageerror') {
      requireExactKeys(entry, new Set(['type', 'name', 'text']), 'page error entry');
      requireShortString(entry.name, 'page error name', 256);
    } else {
      throw new Error('Browser console diagnostics contain an unknown event type.');
    }
    requireShortString(entry.text, 'browser console text', 4000, true);
  }
}

function requireRecord(value, label) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error(`${label} must be an object.`);
  }
}

function requireExactKeys(value, allowed, label) {
  if (Object.keys(value).some((key) => !allowed.has(key))) {
    throw new Error(`${label} contains an unexpected field.`);
  }
  if ([...allowed].some((key) => !(key in value))) {
    throw new Error(`${label} is missing a required field.`);
  }
}

function requireShortString(value, label, maxLength, allowEmpty = false) {
  if (typeof value !== 'string' || (!allowEmpty && value.length === 0) || value.length > maxLength) {
    throw new Error(`${label} must be a bounded string.`);
  }
}

function countStatuses(tests) {
  const counts = {};
  for (const test of tests) {
    counts[test.status] = (counts[test.status] ?? 0) + 1;
  }
  return counts;
}
