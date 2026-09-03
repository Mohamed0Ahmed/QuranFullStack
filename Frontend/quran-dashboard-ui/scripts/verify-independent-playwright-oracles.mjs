import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const repositoryRoot = resolve(process.cwd(), '../..');
const quran = readOracle('quran-fidelity.json');
const phraseSearch = readOracle('phrase-search.json');

assert.equal(quran.contractVersion, 1);
assert.equal(quran.review.authority, 'source-review');
assert.match(quran.review.method, /not generated from the runtime database/i);
assertSourceIdentities(quran.sourceIdentities);

assert.equal(phraseSearch.contractVersion, 1);
assert.equal(phraseSearch.review.authority, 'source-review');
assert.match(phraseSearch.review.method, /not generated from the runtime database/i);
assertSourceIdentities(phraseSearch.sourceIdentities);
assert.equal(phraseSearch.query.raw, 'بسم الله الرحمن الرحيم');
assert.deepEqual(phraseSearch.repetitions.verseKeys, ['1:1', '27:30']);
assert.deepEqual(phraseSearch.similarity.verseKeys, ['1:1', '27:30', '11:41']);

for (const [name, oracle] of Object.entries({ quran, phraseSearch })) {
  const serialized = JSON.stringify(oracle);
  for (const forbidden of [
    'artifactId',
    'artifactVersion',
    'activeBuildId',
    'containerDigest',
    'dumpSha256',
    'sourceFingerprint',
  ]) {
    assert.ok(!serialized.includes(forbidden), `${name} oracle must not contain ${forbidden}`);
  }
}

for (const file of [
  'mushaf-reader.e2e.ts',
  'mushaf-ayah-study.e2e.ts',
  'phrase-search-read.e2e.ts',
  'phrase-search-unavailable-stale.e2e.ts',
]) {
  const source = readFileSync(resolve(process.cwd(), 'e2e', file), 'utf8');
  assert.doesNotMatch(source, /test-artifacts|compact-|type:\s*'artifact'|type:\s*'read-only'/);
  assert.match(source, /type:\s*'canonical-read'/);
  assert.match(source, /fixture-policy[^\n]+canonical-read-only/);
}

const statefulPhraseSource = readFileSync(
  resolve(process.cwd(), 'e2e/phrase-search-available.e2e.ts'),
  'utf8',
);
assert.match(statefulPhraseSource, /test-oracles\/phrase-search\.json/);
assert.doesNotMatch(statefulPhraseSource, /test-artifacts\/.+\/(?:oracle|manifest)\.json/);

console.log('Independent Playwright oracle contract passed.');

function readOracle(file) {
  return JSON.parse(readFileSync(resolve(repositoryRoot, 'test-oracles', file), 'utf8'));
}

function assertSourceIdentities(identities) {
  assert.ok(Array.isArray(identities) && identities.length > 0);
  for (const source of identities) {
    assert.match(source.sha256, /^[0-9a-f]{64}$/);
    assert.ok(source.provenance.length > 0);
    assert.doesNotMatch(source.provenance, /artifact|container|dump|provision/i);
  }
}
