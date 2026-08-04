export interface NavItem {
  key: string;
  labelAr: string;
  labelEn: string;
  route: string;
  group: 'primary' | 'more' | 'actions';
  children?: NavItem[];
  queryParams?: Record<string, string>;
}

export const NAV_ITEMS: NavItem[] = [
  { key: 'dashboard', labelAr: 'لوحة التحكم', labelEn: 'Dashboard', route: '/dashboard', group: 'primary' },
  { key: 'mushaf', labelAr: 'قارئ المصحف', labelEn: 'Mushaf Reader', route: '/dashboard/mushaf', group: 'primary' },
  { key: 'words', labelAr: 'الكلمات والجذور', labelEn: 'Words & Roots', route: '/dashboard/words', group: 'primary' },
  { key: 'tafsirs', labelAr: 'التفاسير', labelEn: 'Tafsirs', route: '/tafsirs', group: 'primary' },
  { key: 'abwab', labelAr: 'الأبواب', labelEn: 'Abwab', route: '/abwab', group: 'primary' },
  { key: 'resources', labelAr: 'المصادر', labelEn: 'Resources', route: '/resources', group: 'primary' },
  { key: 'i3rab', labelAr: 'الإعراب', labelEn: 'I\'rab', route: '/i3rab', group: 'more' },
  { key: 'translations', labelAr: 'الترجمات', labelEn: 'Translations', route: '/translations', group: 'more' },
  { key: 'audio', labelAr: 'الصوتيات', labelEn: 'Audio', route: '/audio', group: 'more' },
  { key: 'mutashabihat', labelAr: 'المتشابهات', labelEn: 'Mutashabihat', route: '/mutashabihat', group: 'more' },
  { key: 'settings', labelAr: 'الإعدادات', labelEn: 'Settings', route: '/settings', group: 'actions' },
];
