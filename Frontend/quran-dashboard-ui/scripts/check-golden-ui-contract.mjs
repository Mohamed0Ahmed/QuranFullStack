import { existsSync, readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

const CONTRACT = 'src/app/shared/layout/breakpoints.contract.json';
const SASS_BANDS = 'src/styles/_breakpoints.scss';
const TS_BANDS = 'src/app/shared/layout/breakpoints.ts';
const TAILWIND = 'tailwind.config.js';
const LAYOUT = 'src/styles/_layout.scss';

const GOLDEN_LAYER = [
  'src/styles.scss',
  'src/styles/_tokens.scss',
  'src/styles/_breakpoints.scss',
  'src/styles/_layout.scss',
  'src/styles/_typography.scss',
  'src/styles/_components.scss',
  'src/styles/_forms.scss',
  'src/styles/_utilities.scss',
  TS_BANDS,
  TAILWIND,
];

const TOKEN_OWNER = 'src/styles/_tokens.scss';

const FORBIDDEN = {
  gradient: /(linear|radial|conic)-gradient/g,
  activeTransform: /:active[^{]*\{[^}]*?transform:[ \t]*(?<value>[^;}]+)/g,
  physicalInline:
    /(?:^|[\s;{])(?:border-(?:left|right)|padding-(?:left|right)|margin-(?:left|right))\s*:/gm,
  physicalInset: /(?:^|[\s;{])(?:left|right)\s*:\s*(?!auto)/gm,
  restingShadow: /box-shadow:\s*var\(--qd-shadow(?:-sm)?\)/g,
  hoverLift: /:hover[^{]*\{[^}]*?(?:transform|translate|scale)[ \t]*:[ \t]*(?<value>[^;}]+)/g,
  colourLiteral: /(?:#[0-9a-fA-F]{3,8}\b|\boklch\(|\brgba?\()/g,
};

const LEGACY_ALLOWLIST = [
  {
    file: 'src/styles/_components.scss',
    rule: 'gradient',
    count: 1,
    reason: 'skeleton shimmer sweep; D18 replaces it with the flat opacity pulse',
    retiresIn: 'Phase 2',
  },
  {
    file: 'src/styles/_components.scss',
    rule: 'activeTransform',
    count: 1,
    reason: '.qd-btn active translate; D14 removes it with the F05 action directive',
    retiresIn: 'Phase 2',
  },
  {
    file: 'src/styles/_components.scss',
    rule: 'colourLiteral',
    count: 0,
    reason: 'no colour literal may live outside the token owner',
    retiresIn: 'never',
  },
  {
    file: 'src/styles/_forms.scss',
    rule: 'gradient',
    count: 2,
    reason: 'select chevron drawn from two gradients; D17 replaces it with a flat icon',
    retiresIn: 'Phase 2',
  },
  {
    file: 'src/styles/_tokens.scss',
    rule: 'colourLiteral',
    count: -1,
    reason: 'the token owner is the only place a colour literal may be written',
    retiresIn: 'never',
  },
  {
    file: 'src/styles/_layout.scss',
    rule: 'domainSelector',
    count: 1,
    reason: '.qd-explorer-frame legacy alias kept until every route declares a named intent',
    retiresIn: 'Phase 11',
  },
];

const QD_STATE_CONSUMER_BASELINE = 53;

const CANONICAL_LAYOUT_SELECTORS = new Set([
  '.qd-page-shell--protected-mushaf',
  '.qd-page-split--mushaf',
]);

const DOMAIN_WORDS = [
  'words',
  'word-type',
  'root',
  'lemma',
  'stem',
  'abwab',
  'access',
  'mushaf',
  'dashboard',
  'explorer',
  'ayah',
  'surah',
  'template',
];

const failures = [];
const notes = [];

function readFile(relative) {
  const absolute = path.join(projectRoot, relative);
  if (!existsSync(absolute)) {
    failures.push(`missing required file: ${relative}`);
    return '';
  }
  return readFileSync(absolute, 'utf8');
}

function countMatches(text, expression) {
  const scoped = new RegExp(expression.source, expression.flags);
  let count = 0;
  for (const match of text.matchAll(scoped)) {
    if (match.groups?.value !== undefined && match.groups.value.trim() === 'none') {
      continue;
    }
    count += 1;
  }
  return count;
}

function allowanceFor(file, rule) {
  const entry = LEGACY_ALLOWLIST.find((row) => row.file === file && row.rule === rule);
  return entry ? entry.count : 0;
}

function walk(relativeDirectory, suffixes) {
  const absolute = path.join(projectRoot, relativeDirectory);
  if (!existsSync(absolute)) {
    return [];
  }
  return readdirSync(absolute, { recursive: true })
    .map((entry) => `${relativeDirectory}/${String(entry).split(path.sep).join('/')}`)
    .filter((entry) => suffixes.some((suffix) => entry.endsWith(suffix)));
}

const contractRaw = readFile(CONTRACT);
const bands = contractRaw ? JSON.parse(contractRaw) : {};

function checkContractShape() {
  const required = ['compactMax', 'mediumMin', 'mediumMax', 'wideMin', 'widePlusMin'];
  for (const key of required) {
    if (typeof bands[key] !== 'number') {
      failures.push(`${CONTRACT} is missing numeric "${key}"`);
    }
  }
  if (bands.mediumMin !== bands.compactMax + 1) {
    failures.push(`${CONTRACT}: mediumMin must be compactMax + 1`);
  }
  if (bands.wideMin !== bands.mediumMax + 1) {
    failures.push(`${CONTRACT}: wideMin must be mediumMax + 1`);
  }
  if (!(bands.widePlusMin > bands.wideMin)) {
    failures.push(`${CONTRACT}: widePlusMin must be greater than wideMin`);
  }
  if (bands.widePlusIsStructural !== false) {
    failures.push(`${CONTRACT}: wide-plus is a measure enhancement, not a structural band`);
  }
}

function bandValues() {
  return new Set([
    bands.compactMax,
    bands.mediumMin,
    bands.mediumMax,
    bands.wideMin,
    bands.widePlusMin,
  ]);
}

function checkSingleBandTruth() {
  const sass = readFile(SASS_BANDS);
  const declared = [...sass.matchAll(/^\$([\w-]+):\s*([^;]+);/gm)];
  const literals = declared.filter(([, , value]) => /\d/.test(value));
  const values = bandValues();
  for (const [, name, value] of literals) {
    const px = Number.parseInt(value.trim(), 10);
    if (!values.has(px)) {
      failures.push(
        `${SASS_BANDS}: $${name} = ${value.trim()} is not one of the contract band values`,
      );
    }
  }
  const sassPxCount = literals.length;
  if (sassPxCount !== values.size) {
    failures.push(
      `${SASS_BANDS}: expected exactly ${values.size} px literals (one per band boundary), found ${sassPxCount}`,
    );
  }

  const ts = readFile(TS_BANDS);
  if (!ts.includes("from './breakpoints.contract.json'")) {
    failures.push(`${TS_BANDS} must import the band values from ${CONTRACT}`);
  }
  if (/\b\d{3,4}\s*px/.test(ts) || /:\s*\d{3,4}\b/.test(ts)) {
    failures.push(`${TS_BANDS} must not restate a band value as a literal`);
  }

  const tailwind = readFile(TAILWIND);
  if (!tailwind.includes('breakpoints.contract.json')) {
    failures.push(`${TAILWIND} must read its screens from ${CONTRACT}`);
  }
  for (const value of values) {
    if (new RegExp(`['"\`]${value}px`).test(tailwind)) {
      failures.push(`${TAILWIND} restates band value ${value}px as a literal`);
    }
  }
}

function checkForbiddenPatterns() {
  for (const file of GOLDEN_LAYER) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    for (const [rule, expression] of Object.entries(FORBIDDEN)) {
      if (rule === 'colourLiteral' && file !== TOKEN_OWNER) {
        const allowed = allowanceFor(file, rule);
        const found = countMatches(text, expression);
        if (found > allowed) {
          failures.push(
            `${file}: ${found} colour literal(s) outside ${TOKEN_OWNER}; allowed ${allowed}`,
          );
        }
        continue;
      }
      if (rule === 'colourLiteral') {
        continue;
      }
      const allowed = allowanceFor(file, rule);
      const found = countMatches(text, expression);
      if (found > allowed) {
        failures.push(`${file}: ${found} ${rule} occurrence(s); allowlist permits ${allowed}`);
      }
      if (found < allowed) {
        failures.push(
          `${file}: allowlist for ${rule} is stale (found ${found}, records ${allowed}); lower the recorded count`,
        );
      }
    }
  }
}

function checkRawBreakpoints() {
  const values = bandValues();
  for (const file of GOLDEN_LAYER) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    for (const match of text.matchAll(/@media[^{]*?(\d{2,4})px/g)) {
      const px = Number.parseInt(match[1], 10);
      if (!values.has(px)) {
        failures.push(`${file}: raw breakpoint ${px}px is not a named band boundary`);
      }
    }
    for (const match of text.matchAll(/@media[^{]*?\$([\w-]+)/g)) {
      if (!match[1].startsWith('qd-bp-')) {
        failures.push(`${file}: media query uses non-band variable $${match[1]}`);
      }
    }
  }
}

function checkPageShellContract() {
  const layout = readFile(LAYOUT);
  const required = [
    '.qd-page-shell',
    '.qd-page-shell--capped-reading',
    '.qd-page-shell--full-data',
    '.qd-page-shell--split-workspace',
    '.qd-page-shell--protected-mushaf',
    '.qd-page-rail--s',
    '.qd-page-rail--m',
    '.qd-page-rail--l',
    '.qd-grid--destinations',
    '.qd-grid--curriculum',
    '.qd-grid--doors',
    '.qd-grid--permission-groups',
  ];
  for (const selector of required) {
    if (!layout.includes(`${selector} `) && !layout.includes(`${selector},`) && !layout.includes(`${selector}{`)) {
      failures.push(`${LAYOUT} does not declare ${selector}`);
    }
  }

  if (/\.qd-page\s*\{[^}]*padding-inline/.test(layout)) {
    failures.push(`${LAYOUT}: .qd-page must stay block-rhythm-only and own no inline gutter`);
  }

  const gutterOwners = countMatches(layout, /padding-inline:\s*var\(--qd-page-gutter\)/g);
  if (gutterOwners !== 4) {
    failures.push(
      `${LAYOUT}: expected exactly 4 gutter declarations (page shell, .qd-container, the frame aliases, the legacy header compat rule); found ${gutterOwners}`,
    );
  }

  const domainAllowance = allowanceFor(LAYOUT, 'domainSelector');
  const domainHits = [...layout.matchAll(/\.qd-[\w-]+/g)]
    .map((match) => match[0])
    .filter((selector) => !CANONICAL_LAYOUT_SELECTORS.has(selector))
    .filter((selector) => DOMAIN_WORDS.some((word) => selector.includes(word)));
  const uniqueDomainHits = [...new Set(domainHits)];
  if (uniqueDomainHits.length > domainAllowance) {
    failures.push(
      `${LAYOUT}: domain-named selector(s) in the layout layer: ${uniqueDomainHits.join(', ')}`,
    );
  }
}

function checkQdStateNoGrowth() {
  const files = walk('src/app', ['.html']);
  let consumers = 0;
  for (const file of files) {
    consumers += countMatches(readFileSync(path.join(projectRoot, file), 'utf8'), /<qd-state/g);
  }
  if (consumers > QD_STATE_CONSUMER_BASELINE) {
    failures.push(
      `qd-state adapter grew: ${consumers} template consumers, baseline ${QD_STATE_CONSUMER_BASELINE}`,
    );
  }
  notes.push(`qd-state template consumers: ${consumers} (baseline ${QD_STATE_CONSUMER_BASELINE})`);
}

checkContractShape();
checkSingleBandTruth();
checkForbiddenPatterns();
checkRawBreakpoints();
checkPageShellContract();
checkQdStateNoGrowth();

console.log('check-golden-ui-contract');
console.log(
  `bands: compact <=${bands.compactMax}, medium ${bands.mediumMin}-${bands.mediumMax}, wide >=${bands.wideMin}, wide-plus >=${bands.widePlusMin}`,
);
console.log(`golden layer files scanned: ${GOLDEN_LAYER.length}`);
console.log(`legacy allowlist entries: ${LEGACY_ALLOWLIST.length}`);
for (const note of notes) {
  console.log(note);
}

if (failures.length > 0) {
  console.log('');
  console.log(`FAIL check-golden-ui-contract: ${failures.length} problem(s)`);
  for (const failure of failures) {
    console.log(`  - ${failure}`);
  }
  process.exit(1);
}

console.log('');
console.log('PASS check-golden-ui-contract');
