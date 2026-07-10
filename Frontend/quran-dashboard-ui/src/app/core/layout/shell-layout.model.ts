export const SHELL_LAYOUT_PAGE_SCROLL = 'page-scroll' as const;

export type ShellLayout = typeof SHELL_LAYOUT_PAGE_SCROLL;

export const SHELL_MAIN_PAGE_SCROLL_CLASS = 'qd-shell-main--page-scroll';

export function isPageScrollShellLayout(layout: unknown): layout is ShellLayout {
  return layout === SHELL_LAYOUT_PAGE_SCROLL;
}
