import { NAV_ITEMS, NavItem } from './nav-items';
import { ABWAB_ROUTE_PATH, SETTINGS_ACCESS_ROUTE_PATH } from './route-paths';
import { WORDS_MENU_ITEMS } from './words-nav-items';

const ABWAB_MENU_ITEMS: readonly NavItem[] = [
  {
    key: 'abwab-home',
    labelAr: 'الرئيسية',
    labelEn: 'Home',
    route: ABWAB_ROUTE_PATH,
    icon: 'tree',
    group: 'primary',
  },
  {
    key: 'abwab-templates',
    labelAr: 'قوالب الأبواب',
    labelEn: 'Templates',
    route: `${ABWAB_ROUTE_PATH}/templates`,
    icon: 'template',
    group: 'primary',
  },
  {
    key: 'abwab-archive',
    labelAr: 'الأرشيف',
    labelEn: 'Archive',
    route: ABWAB_ROUTE_PATH,
    icon: 'archive',
    group: 'primary',
    queryParams: { archive: '1' },
  },
];

const childrenByParentKey: Record<string, NavItem[]> = {
  words: [...WORDS_MENU_ITEMS],
  abwab: [...ABWAB_MENU_ITEMS],
  settings: [
    {
      key: 'settings-access',
      labelAr: 'إدارة الوصول',
      labelEn: 'Access Management',
      route: SETTINGS_ACCESS_ROUTE_PATH,
      icon: 'user',
      group: 'actions',
    },
  ],
};

export const NAV_MENU: NavItem[] = NAV_ITEMS.map((item) => {
  const children = childrenByParentKey[item.key];
  return children ? { ...item, children } : item;
});
