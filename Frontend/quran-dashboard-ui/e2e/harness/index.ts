// Reusable Playwright harness for the Abwab UI Kits (FR-004, §15.2 gate 6). Later specs (029+) import
// `test`/`expect` and these scenario helpers; this feature ships only the scaffolding, no domain tests.
export { test, expect } from '@playwright/test';

export * as rtl from './rtl';
export * as a11y from './a11y';
export * as keyboard from './keyboard';
export * as virtualization from './virtualization';
export * as dialog from './dialog';
