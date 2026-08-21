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
  restingShadow: /box-shadow:[^;}]*var\(--qd-shadow(?!-layer\b)[\w-]*\)/g,
  hoverLift: /:hover[^{]*\{[^}]*?(?:transform|translate|scale)[ \t]*:[ \t]*(?<value>[^;}]+)/g,
  colourLiteral: /(?:#[0-9a-fA-F]{3,8}\b|\boklch\(|\brgba?\()/g,
};

const LEGACY_ALLOWLIST = [
  {
    file: 'src/styles/_components.scss',
    rule: 'colourLiteral',
    count: 0,
    reason: 'no colour literal may live outside the token owner',
    retiresIn: 'never',
  },
  {
    file: 'src/styles/_tokens.scss',
    rule: 'colourLiteral',
    count: -1,
    reason: 'the token owner is the only place a colour literal may be written',
    retiresIn: 'never',
  },
  {
    file: 'src/styles/_tokens.scss',
    rule: 'gradient',
    count: 2,
    reason: 'the fixed multi-door Mushaf word and ayah-marker gradient tokens',
    retiresIn: 'never',
  },
];

const SCOPED_GRADIENT_TOKEN_CONSUMERS = [
  {
    token: '--qd-door-highlight-multi-gradient',
    file: 'src/app/features/mushaf/components/mushaf-word/mushaf-word.component.ts',
    count: 1,
  },
  {
    token: '--qd-door-marker-multi-gradient',
    file: 'src/app/features/mushaf/components/mushaf-word/mushaf-word.component.scss',
    count: 1,
  },
];

const QD_STATE_CONSUMER_BASELINE = 0;

const QD_STATE_ADAPTER_DIRECTORY = 'src/app/shared/ui/state';

const INTERACTION_OWNERS = [
  'src/app/shared/ui/tabs/tabs.component.ts',
  'src/app/shared/ui/tabs/tab.directive.ts',
  'src/app/shared/ui/result-list/result-list.directive.ts',
  'src/app/shared/ui/details-workspace/details-workspace.component.ts',
  'src/app/shared/ui/pagination/pagination.component.ts',
  'src/app/shared/ui/modal-shell/modal-shell.component.ts',
  'src/app/shared/ui/floating-layer/floating-layer.directive.ts',
  'src/app/shared/ui/floating-layer/floating-layer-placement.ts',
  'src/app/shared/ui/chip/chip.component.ts',
];

const MODAL_SHELL_STYLES = 'src/app/shared/ui/modal-shell/modal-shell.component.scss';

const MODAL_SHELL_VARIANTS = ['confirm', 'form', 'wide', 'overlay'];

// The only surfaces allowed to carry a resting elevation, and then only --qd-shadow-layer.
const FLOATING_ELEVATION_OWNERS = [
  '.qd-modal-shell',
  '.qd-floating-layer',
  '.qd-nav__menu',
  '.detail-modal-shell__restore',
];

const ELEVATION_TOKEN = /box-shadow:[^;}]*var\((--qd-shadow[\w-]*)\)/g;

// A class that names a dialog/modal surface, e.g. .qd-modal, .explorer-detail-modal.
const MODAL_MARKER_CLASS = /(^|[.\s])[a-z0-9-]*modal[a-z0-9-]*(\b|[.:\s])/i;

const INLINE_SIZING_DECLARATION = /^(?:max-|min-)?(?:width|inline-size)\s*:/i;

const SELECTION_EDGE_FILES = [
  'src/styles/_components.scss',
  'src/styles/_explorer-tables.scss',
  'src/styles/_explorer-detail-lists.scss',
  'src/styles/_words-explorer-layout.scss',
];

const ENTRANCE_MOTION_FILES = [
  'src/styles/_components.scss',
  'src/styles/_words-explorer-layout.scss',
  'src/styles/_explorer-tables.scss',
];

const ASYNC_OWNERS = [
  'src/app/shared/ui/skeleton/skeleton-rows.component.ts',
  'src/app/shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component.ts',
  'src/app/shared/ui/refreshing-indicator/refreshing-indicator.component.ts',
  'src/app/shared/ui/empty-state/empty-state.component.ts',
  'src/app/shared/ui/error-state/error-state.component.ts',
  'src/app/shared/ui/notice/notice.component.ts',
];

const CANONICAL_LAYOUT_SELECTORS = new Set([
  '.qd-page-shell--protected-mushaf',
  '.qd-page-split--mushaf',
]);

const PROTECTED_QURAN_SELECTORS = [
  'mushaf-line',
  'mushaf-word',
  'mushaf-marker',
  'segment-rendered-word',
  'mushaf-page-view__text',
];

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

// Blanks comments out in place so every offset (and therefore every line number) is preserved.
function stripStyleComments(text) {
  return text
    .replace(/\/\*[\s\S]*?\*\//g, (match) => match.replace(/[^\n]/g, ' '))
    .replace(/(^|[^:])\/\/[^\n]*/g, (match, prefix) => prefix + ' '.repeat(match.length - prefix.length));
}

function lineAt(text, index) {
  let line = 1;
  for (let cursor = 0; cursor < index; cursor += 1) {
    if (text[cursor] === '\n') {
      line += 1;
    }
  }
  return line;
}

// Nesting-aware block reader: each block keeps its full (possibly multi-line) prelude and the
// declarations written at its own level, so a selector list never has to be read one line at a time.
function parseStyleBlocks(source) {
  const text = stripStyleComments(source);
  const blocks = [];
  const stack = [];
  let segmentStart = 0;

  const closeSegment = (end) => {
    const declaration = text.slice(segmentStart, end).trim();
    const owner = stack[stack.length - 1];
    if (owner && declaration) {
      owner.declarations.push(declaration);
    }
  };

  for (let index = 0; index < text.length; index += 1) {
    const character = text[index];
    if (character === '{') {
      const prelude = text.slice(segmentStart, index);
      const offset = prelude.length - prelude.trimStart().length;
      const block = {
        prelude: prelude.trim(),
        line: lineAt(text, segmentStart + offset),
        declarations: [],
      };
      blocks.push(block);
      stack.push(block);
      segmentStart = index + 1;
    } else if (character === '}') {
      closeSegment(index);
      stack.pop();
      segmentStart = index + 1;
    } else if (character === ';') {
      closeSegment(index);
      segmentStart = index + 1;
    }
  }
  return blocks;
}

function selectorsOf(block) {
  if (block.prelude.startsWith('@') || block.prelude === '') {
    return [];
  }
  return block.prelude
    .split(',')
    .map((selector) => selector.trim())
    .filter(Boolean);
}

function withoutPseudos(selector) {
  return selector.replace(/::?[\w-]+(\([^)]*\))?/g, ' ');
}

function classTokens(selector) {
  return [...withoutPseudos(selector).matchAll(/\.[\w-]+/g)].map((match) => match[0]);
}

function hasOwnerClass(selector, owner) {
  const expression = new RegExp(`(^|[^\\w-])${owner.replace('.', '\\.')}(?![\\w-])`);
  return expression.test(withoutPseudos(selector));
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

function checkScopedGradientTokenConsumers() {
  const sourceFiles = walk('src', ['.scss', '.ts', '.html']);

  for (const contract of SCOPED_GRADIENT_TOKEN_CONSUMERS) {
    const expression = new RegExp(`var\\(${contract.token}\\)`, 'g');
    for (const file of sourceFiles) {
      const found = countMatches(readFile(file), expression);
      const allowed = file === contract.file ? contract.count : 0;
      if (found !== allowed) {
        failures.push(
          `${file}: ${found} use(s) of ${contract.token}; contract permits ${allowed}`,
        );
      }
    }
  }

  notes.push(`scoped gradient token consumers: ${SCOPED_GRADIENT_TOKEN_CONSUMERS.length}`);
}

function checkRawBreakpoints() {
  const values = bandValues();
  const scanned = new Set([...GOLDEN_LAYER, ...walk('src', ['.scss'])]);
  let inspected = 0;
  for (const file of scanned) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    inspected += 1;
    for (const prelude of text.matchAll(/@media([^{]*)\{/g)) {
      for (const feature of prelude[1].matchAll(
        /(min|max)-(?:width|inline-size)\s*:\s*([\d.]+)(px|rem|em)/g,
      )) {
        const px = feature[3] === 'px' ? Number.parseFloat(feature[2]) : Number.parseFloat(feature[2]) * 16;
        if (!values.has(px)) {
          failures.push(
            `${file}: raw responsive threshold ${feature[2]}${feature[3]} is not a named band boundary (D11)`,
          );
        }
      }
      for (const variable of prelude[1].matchAll(/\$([\w-]+)/g)) {
        if (!variable[1].startsWith('qd-bp-')) {
          failures.push(`${file}: media query uses non-band variable $${variable[1]}`);
        }
      }
    }
  }
  notes.push(`stylesheets scanned for raw responsive thresholds: ${inspected}`);
}

function checkProtectedQuranReach() {
  const violations = [];
  for (const file of GOLDEN_LAYER) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    if (!file.endsWith('.scss')) {
      for (const token of PROTECTED_QURAN_SELECTORS) {
        if (text.includes(token)) {
          violations.push(`${file}: names "${token}"`);
        }
      }
      continue;
    }
    for (const block of parseStyleBlocks(text)) {
      for (const selector of selectorsOf(block)) {
        const bare = withoutPseudos(selector);
        if (PROTECTED_QURAN_SELECTORS.some((token) => bare.includes(token))) {
          violations.push(`${file}:${block.line} "${selector}"`);
        }
      }
    }
  }
  if (violations.length > 0) {
    failures.push(
      `the Golden layer may not reach Quran renderer descendants (G02): ${violations.join('; ')}`,
    );
  }
  notes.push(`protected Quran selector tokens guarded: ${PROTECTED_QURAN_SELECTORS.length}`);
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
  if (gutterOwners !== 1) {
    failures.push(
      `${LAYOUT}: the page shell is the sole route-gutter owner (D01); found ${gutterOwners} declaration(s)`,
    );
  }

  const strayGutterOwners = [];
  for (const file of walk('src', ['.scss'])) {
    if (file === LAYOUT) {
      continue;
    }
    const text = readFileSync(path.join(projectRoot, file), 'utf8');
    if (/padding-inline:\s*var\(--qd-page-gutter\)/.test(text)) {
      strayGutterOwners.push(file);
    }
  }
  if (strayGutterOwners.length > 0) {
    failures.push(
      `route gutter declared outside ${LAYOUT} (D01): ${strayGutterOwners.join(', ')}`,
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

function checkQdStateRetired() {
  const files = walk('src/app', ['.html', '.ts']);
  let consumers = 0;
  for (const file of files) {
    const text = readFileSync(path.join(projectRoot, file), 'utf8');
    consumers += countMatches(text, /<qd-state[\s/>]/g) + countMatches(text, /\bQdStateComponent\b/g);
  }
  if (consumers > QD_STATE_CONSUMER_BASELINE) {
    failures.push(
      `qd-state adapter grew: ${consumers} consumer(s), baseline ${QD_STATE_CONSUMER_BASELINE}`,
    );
  }
  if (existsSync(path.join(projectRoot, QD_STATE_ADAPTER_DIRECTORY))) {
    failures.push(
      `${QD_STATE_ADAPTER_DIRECTORY} was retired in Phase 11 (D39); it must not come back`,
    );
  }
  notes.push(`qd-state consumers: ${consumers} (baseline ${QD_STATE_CONSUMER_BASELINE}, adapter deleted)`);
}

function checkAsyncOwners() {
  for (const owner of ASYNC_OWNERS) {
    if (!existsSync(path.join(projectRoot, owner))) {
      failures.push(`missing F12 async owner: ${owner}`);
    }
  }

  notes.push(`F12 async owners present: ${ASYNC_OWNERS.length}`);
}

function checkInteractionOwners() {
  for (const owner of INTERACTION_OWNERS) {
    if (!existsSync(path.join(projectRoot, owner))) {
      failures.push(`missing Phase 3 interaction owner: ${owner}`);
    }
  }
  notes.push(`interaction owners present: ${INTERACTION_OWNERS.length}`);
}

function checkModalWidthVocabulary() {
  const styles = readFile(MODAL_SHELL_STYLES);
  if (!styles) {
    return;
  }
  const declared = [...styles.matchAll(/\.qd-modal-shell--([a-z-]+)/g)].map((match) => match[1]);
  const unique = [...new Set(declared)].sort();
  const expected = [...MODAL_SHELL_VARIANTS].sort();
  if (unique.join(',') !== expected.join(',')) {
    failures.push(
      `${MODAL_SHELL_STYLES}: modal widths must be exactly ${expected.join('/')}; found ${unique.join('/') || 'none'}`,
    );
  }
  notes.push(`modal shell widths: ${unique.join(', ')}`);

  const sanctioned = new Set(MODAL_SHELL_VARIANTS.map((variant) => `.qd-modal-shell--${variant}`));
  const escaped = [];
  for (const file of GOLDEN_LAYER) {
    if (!file.endsWith('.scss') || file === MODAL_SHELL_STYLES) {
      continue;
    }
    const text = readFile(file);
    if (!text) {
      continue;
    }
    for (const block of parseStyleBlocks(text)) {
      const sizes = block.declarations.some((declaration) =>
        INLINE_SIZING_DECLARATION.test(declaration),
      );
      if (!sizes) {
        continue;
      }
      for (const selector of selectorsOf(block)) {
        if (!MODAL_MARKER_CLASS.test(withoutPseudos(selector))) {
          continue;
        }
        const markers = classTokens(selector).filter(
          (token) => /modal/i.test(token) && !sanctioned.has(token),
        );
        if (markers.length > 0) {
          escaped.push(`${file}:${block.line} "${selector}"`);
        }
      }
    }
  }
  if (escaped.length > 0) {
    failures.push(
      `dialog geometry declared outside ${MODAL_SHELL_STYLES}; only the four named widths may size a modal: ${escaped.join('; ')}`,
    );
  }
}

function checkRestingElevation() {
  const goldenLayer = new Set(GOLDEN_LAYER);
  const violations = [];
  for (const file of walk('src', ['.scss'])) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    for (const block of parseStyleBlocks(text)) {
      for (const declaration of block.declarations) {
        for (const match of declaration.matchAll(ELEVATION_TOKEN)) {
          const token = match[1];
          const selectors = selectorsOf(block);
          const owned =
            token === '--qd-shadow-layer' &&
            selectors.length > 0 &&
            selectors.every((selector) =>
              FLOATING_ELEVATION_OWNERS.some((owner) => hasOwnerClass(selector, owner)),
            );
          if (owned) {
            continue;
          }
          // A non-layer elevation inside the Golden layer is already counted by FORBIDDEN.restingShadow.
          if (goldenLayer.has(file) && token !== '--qd-shadow-layer') {
            continue;
          }
          violations.push(`${file}:${block.line} "${block.prelude}" uses var(${token})`);
        }
      }
    }
  }
  if (violations.length > 0) {
    failures.push(
      `resting elevation outside the floating owners (${FLOATING_ELEVATION_OWNERS.join(', ')}): ${violations.join('; ')}`,
    );
  }
  notes.push(`floating elevation owners: ${FLOATING_ELEVATION_OWNERS.length}`);
}

function checkSelectionEdgeIsLogical() {
  for (const file of SELECTION_EDGE_FILES) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    for (const match of text.matchAll(/box-shadow:\s*inset\s+-?\d+px\s+0/g)) {
      failures.push(
        `${file}: "${match[0]}" is a physical selected edge; use the logical inline-start thread`,
      );
    }
  }
}

function checkNoEntranceMotion() {
  for (const file of ENTRANCE_MOTION_FILES) {
    const text = readFile(file);
    if (!text) {
      continue;
    }
    const found = [];
    for (const match of text.matchAll(/@keyframes\s+([\w-]+)\s*\{([\s\S]*?)\n\}/g)) {
      if (/from\s*\{[^}]*(transform|translate)/.test(match[2])) {
        found.push(match[1]);
      }
    }
    const allowed = allowanceFor(file, 'entranceMotion');
    if (found.length > allowed) {
      failures.push(
        `${file}: @keyframes ${found.join(', ')} is decorative entrance motion (D19); allowlist permits ${allowed}`,
      );
    } else if (found.length < allowed) {
      failures.push(
        `${file}: allowlist for entranceMotion is stale (found ${found.length}, records ${allowed}); lower the recorded count`,
      );
    }
  }
}

function checkNoArtificialTabStops() {
  const templates = walk('src/app/shared', ['.html']);
  for (const file of templates) {
    const text = readFileSync(path.join(projectRoot, file), 'utf8');
    for (const match of text.matchAll(/<[^>]*qd-truncate[^>]*>/g)) {
      if (/tabindex\s*=\s*["']?0/.test(match[0])) {
        failures.push(`${file}: a truncated node carries tabindex="0"; use the §8.1 disclosure ladder`);
      }
    }
  }
}

checkContractShape();
checkSingleBandTruth();
checkForbiddenPatterns();
checkScopedGradientTokenConsumers();
checkRawBreakpoints();
checkPageShellContract();
checkQdStateRetired();
checkAsyncOwners();
checkProtectedQuranReach();
checkInteractionOwners();
checkModalWidthVocabulary();
checkRestingElevation();
checkSelectionEdgeIsLogical();
checkNoEntranceMotion();
checkNoArtificialTabStops();

console.log('check-golden-ui-contract');
console.log(
  `bands: compact <=${bands.compactMax}, medium ${bands.mediumMin}-${bands.mediumMax}, wide >=${bands.wideMin}, wide-plus >=${bands.widePlusMin}`,
);
console.log(`golden layer files scanned: ${GOLDEN_LAYER.length}`);
console.log(`pattern allowlist entries: ${LEGACY_ALLOWLIST.length}`);
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
