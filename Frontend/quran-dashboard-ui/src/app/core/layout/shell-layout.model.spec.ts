import { describe, expect, it } from 'vitest';

import { isPageScrollShellLayout, SHELL_LAYOUT_PAGE_SCROLL } from './shell-layout.model';

describe('shell-layout.model', () => {
  it('recognizes the page-scroll shell layout token', () => {
    expect(isPageScrollShellLayout(SHELL_LAYOUT_PAGE_SCROLL)).toBe(true);
  });

  it('rejects unknown shell layout values', () => {
    expect(isPageScrollShellLayout(undefined)).toBe(false);
    expect(isPageScrollShellLayout('full-height')).toBe(false);
  });
});
