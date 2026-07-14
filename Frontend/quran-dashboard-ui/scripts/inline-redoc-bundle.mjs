// Post-processes the redocly build-docs output so the static API reference is fully
// offline: replaces the hardcoded cdn.redocly.com <script src> tag with the local
// redoc.standalone.js bundle (pinned dev-dependency), inlined into the HTML.
// Run from the frontend root via `npm run docs:api`.
import { readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';

const htmlPath = '../../docs/api-reference/index.html';
const require = createRequire(import.meta.url);
const bundlePath = require.resolve('redoc/bundles/redoc.standalone.js');

const html = readFileSync(htmlPath, 'utf8');
const cdnScriptTag = /<script src="https:\/\/cdn\.redocly\.com\/redoc\/[^"]+"[^>]*><\/script>/;
if (!cdnScriptTag.test(html)) {
  throw new Error('cdn.redocly.com redoc script tag not found — redocly build-docs template changed?');
}

// Escape closing-tag sequences inside the bundle so the inline <script> cannot be
// terminated early; use a replacer function so `$` sequences in the bundle stay literal.
const bundle = readFileSync(bundlePath, 'utf8').replace(/<\/script/gi, '<\\/script');
writeFileSync(htmlPath, html.replace(cdnScriptTag, () => `<script>${bundle}</script>`));
console.log(`Inlined ${bundlePath} into ${htmlPath}`);
