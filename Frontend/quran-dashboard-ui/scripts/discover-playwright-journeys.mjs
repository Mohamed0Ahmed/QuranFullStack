import { relative } from 'node:path';

const JOURNEY_ANNOTATIONS = new Set([
  'artifact',
  'critical',
  'journey',
  'mobile',
  'mutating',
  'read-only',
]);
const PLAYWRIGHT_ANNOTATIONS = new Set(['fail', 'fixme', 'skip', 'slow']);
const IDENTIFIER_PATTERN = /^[a-z0-9]+(?:[._-][a-z0-9]+)*$/;

function fail(message) {
  throw new Error(`Playwright journey discovery failed: ${message}`);
}

function annotationValues(annotations, type) {
  return annotations.filter((annotation) => annotation.type === type);
}

function describeTest(test, file) {
  return `${file}:${test.location.line} (${test.parent.project()?.name ?? 'unknown'}: ${test.title})`;
}

function singleAnnotation(annotations, type, location, required) {
  const matches = annotationValues(annotations, type);
  if (required && matches.length === 0) {
    fail(`${location} is missing required ${type} annotation`);
  }
  if (matches.length > 1) {
    fail(`${location} has duplicate ${type} annotations`);
  }

  return matches[0];
}

function requireFlag(annotations, type, location) {
  const annotation = singleAnnotation(annotations, type, location, true);
  if (annotation.description !== undefined) {
    fail(`${location} has unsupported description on ${type} annotation`);
  }
}

function optionalFlag(annotations, type, location) {
  const annotation = singleAnnotation(annotations, type, location, false);
  if (annotation?.description !== undefined) {
    fail(`${location} has unsupported description on ${type} annotation`);
  }
  return annotation !== undefined;
}

function requireIdentifier(annotations, type, location) {
  const identifier = singleAnnotation(annotations, type, location, true).description;
  if (!identifier || !IDENTIFIER_PATTERN.test(identifier)) {
    fail(`${location} has unsupported ${type} identifier "${identifier ?? ''}"`);
  }
  return identifier;
}

function validateMetadata(test, rootDir) {
  const annotations = test.annotations ?? [];
  const file = relative(rootDir, test.location.file);
  const location = describeTest(test, file);

  for (const annotation of annotations) {
    if (!JOURNEY_ANNOTATIONS.has(annotation.type) && !PLAYWRIGHT_ANNOTATIONS.has(annotation.type)) {
      fail(`${location} has unsupported annotation "${annotation.type}"`);
    }
  }

  const journeyMetadata = annotations.filter((annotation) =>
    JOURNEY_ANNOTATIONS.has(annotation.type),
  );
  if (journeyMetadata.length === 0) {
    return null;
  }

  requireFlag(annotations, 'critical', location);
  const mobile = optionalFlag(annotations, 'mobile', location);
  const mutating = annotationValues(annotations, 'mutating');
  const readOnly = annotationValues(annotations, 'read-only');

  if (mutating.length > 0 && readOnly.length > 0) {
    fail(`${location} has contradictory mutating and read-only annotations`);
  }
  if (mutating.length === 0 && readOnly.length === 0) {
    fail(`${location} is missing required mutating or read-only annotation`);
  }

  const state = mutating.length > 0 ? 'mutating' : 'read-only';
  requireFlag(annotations, state, location);

  return {
    artifact: requireIdentifier(annotations, 'artifact', location),
    file,
    journey: requireIdentifier(annotations, 'journey', location),
    line: test.location.line,
    mobile,
    project: test.parent.project()?.name ?? 'unknown',
    state,
    title: test.title,
  };
}

function selectCriticalJourneys(suite, rootDir) {
  const journeys = [];
  const journeyLocations = new Map();

  for (const test of suite.allTests()) {
    const journey = validateMetadata(test, rootDir);
    if (!journey) {
      continue;
    }

    const previousLocation = journeyLocations.get(journey.journey);
    const location = `${journey.file}:${journey.line}`;
    if (previousLocation) {
      fail(
        `duplicate journey identifier "${journey.journey}" at ${previousLocation} and ${location}`,
      );
    }

    journeyLocations.set(journey.journey, location);
    journeys.push(journey);
  }

  if (journeys.length === 0) {
    fail('no critical journeys were discovered');
  }

  return journeys.sort((left, right) =>
    `${left.journey}\0${left.project}`.localeCompare(`${right.journey}\0${right.project}`),
  );
}

export default class CriticalJourneyDiscoveryReporter {
  validationFailed = false;

  constructor(options = {}) {
    this.stdout = options.stdout ?? process.stdout;
    this.stderr = options.stderr ?? process.stderr;
  }

  onBegin(config, suite) {
    try {
      const journeys = selectCriticalJourneys(suite, config.rootDir);
      this.stdout.write(`${JSON.stringify(journeys, null, 2)}\n`);
    } catch (error) {
      this.validationFailed = true;
      this.stderr.write(`${error.message}\n`);
    }
  }

  onEnd() {
    if (this.validationFailed) {
      return { status: 'failed' };
    }
    return undefined;
  }
}
