import { NavItem } from './nav-items';
import {
  WORDS_ROUTE_PATH,
  lemmasRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from './route-paths';

// Labels are the menu section names, owned here in core like `NAV_ITEMS`; routes come from
// `route-paths`. `unique` points at the `tashkeel` mode so the link skips the redirect hop.
// Consumed as `NavItem[]` via `nav-menu.ts`, not read directly by the navbar.
export const WORDS_MENU_ITEMS: readonly NavItem[] = [
  { key: 'words-home', labelAr: 'الرئيسية', labelEn: 'Home', route: WORDS_ROUTE_PATH, group: 'primary' },
  { key: 'words-unique', labelAr: 'الكلمات الفريدة', labelEn: 'Unique Words', route: uniqueWordsRoutePath('tashkeel'), group: 'primary' },
  { key: 'words-roots', labelAr: 'الجذور', labelEn: 'Roots', route: rootsRoutePath(), group: 'primary' },
  { key: 'words-lemmas', labelAr: 'الصيغ المعجمية', labelEn: 'Lemmas', route: lemmasRoutePath(), group: 'primary' },
  { key: 'words-stems', labelAr: 'الأصول الصرفية', labelEn: 'Stems', route: stemsRoutePath(), group: 'primary' },
  { key: 'words-types', labelAr: 'أنواع الكلمات', labelEn: 'Word Types', route: wordTypesRoutePath(), group: 'primary' },
];
