export type NavIconName =
  | 'archive'
  | 'book'
  | 'compare'
  | 'dashboard'
  | 'languages'
  | 'lemma'
  | 'library'
  | 'link'
  | 'login'
  | 'logout'
  | 'menu'
  | 'more'
  | 'moon'
  | 'pen'
  | 'root'
  | 'search'
  | 'settings'
  | 'stem'
  | 'sun'
  | 'tag'
  | 'template'
  | 'tree'
  | 'user'
  | 'volume'
  | 'words';

export interface NavItem {
  key: string;
  labelAr: string;
  labelEn: string;
  route: string;
  icon: NavIconName;
  resumePath?: string;
  group: 'primary' | 'more' | 'actions';
  children?: NavItem[];
  queryParams?: Record<string, string>;
}

export const NAV_ITEMS: NavItem[] = [
  {
    key: 'dashboard',
    labelAr: 'لوحة التحكم',
    labelEn: 'Dashboard',
    route: '/dashboard',
    icon: 'dashboard',
    group: 'primary',
  },
  {
    key: 'abwab',
    labelAr: 'الأبواب',
    labelEn: 'Abwab',
    route: '/abwab',
    icon: 'tree',
    group: 'primary',
  },
  {
    key: 'words',
    labelAr: 'الكلمات والجذور',
    labelEn: 'Words & Roots',
    route: '/dashboard/words',
    icon: 'words',
    group: 'primary',
  },
  {
    key: 'mushaf',
    labelAr: 'قارئ المصحف',
    labelEn: 'Mushaf Reader',
    route: '/dashboard/mushaf',
    icon: 'book',
    group: 'primary',
  },
  {
    key: 'tafsirs',
    labelAr: 'التفاسير',
    labelEn: 'Tafsirs',
    route: '/tafsirs',
    icon: 'library',
    group: 'more',
  },
  {
    key: 'resources',
    labelAr: 'المصادر',
    labelEn: 'Resources',
    route: '/resources',
    icon: 'archive',
    group: 'more',
  },
  {
    key: 'i3rab',
    labelAr: 'الإعراب',
    labelEn: 'I\'rab',
    route: '/i3rab',
    icon: 'pen',
    group: 'more',
  },
  {
    key: 'translations',
    labelAr: 'الترجمات',
    labelEn: 'Translations',
    route: '/translations',
    icon: 'languages',
    group: 'more',
  },
  {
    key: 'audio',
    labelAr: 'الصوتيات',
    labelEn: 'Audio',
    route: '/audio',
    icon: 'volume',
    group: 'more',
  },
  {
    key: 'mutashabihat',
    labelAr: 'المتشابهات',
    labelEn: 'Mutashabihat',
    route: '/mutashabihat',
    icon: 'compare',
    group: 'more',
  },
  {
    key: 'settings',
    labelAr: 'الإعدادات',
    labelEn: 'Settings',
    route: '/settings',
    icon: 'settings',
    group: 'actions',
  },
];
