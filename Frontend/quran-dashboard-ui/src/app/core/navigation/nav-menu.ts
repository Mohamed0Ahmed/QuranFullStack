import { NAV_ITEMS, NavItem } from './nav-items';
import { ABWAB_ROUTE_PATH } from './route-paths';
import { WORDS_MENU_ITEMS } from './words-nav-items';

// No exported `/abwab/templates` constant exists — the segment is owned by
// `abwab.routes.ts` — so it is composed from `ABWAB_ROUTE_PATH` here rather than adding a
// second source of truth for one string.
const ABWAB_MENU_ITEMS: readonly NavItem[] = [
  { key: 'abwab-home', labelAr: 'الرئيسية', labelEn: 'Home', route: ABWAB_ROUTE_PATH, group: 'primary' },
  {
    key: 'abwab-templates',
    labelAr: 'قوالب الأبواب',
    labelEn: 'Templates',
    route: `${ABWAB_ROUTE_PATH}/templates`,
    group: 'primary',
  },
  {
    key: 'abwab-archive',
    labelAr: 'الأرشيف',
    labelEn: 'Archive',
    route: ABWAB_ROUTE_PATH,
    group: 'primary',
    queryParams: { archive: '1' },
  },
];

const childrenByParentKey: Record<string, NavItem[]> = {
  words: [...WORDS_MENU_ITEMS],
  abwab: [...ABWAB_MENU_ITEMS],
};

// `route-paths.ts` imports `NAV_ITEMS` and derives every route constant from it at module
// init (`navRoute('dashboard')` etc.) — nesting children into `nav-items.ts` would create an
// import cycle whose init order always hits a TDZ `ReferenceError`. Children attach here
// instead, outside `NAV_ITEMS`; the navbar template branches on `item.children`, never a key.
export const NAV_MENU: NavItem[] = NAV_ITEMS.map((item) => {
  const children = childrenByParentKey[item.key];
  return children ? { ...item, children } : item;
});
