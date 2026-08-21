import { NavItem } from './nav-items';
import {
  WORDS_ROUTE_PATH,
  lemmasRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from './route-paths';

export const WORDS_MENU_ITEMS: readonly NavItem[] = [
  { key: 'words-home', labelAr: 'الرئيسية', labelEn: 'Home', route: WORDS_ROUTE_PATH, group: 'primary' },
  {
    key: 'words-unique',
    labelAr: 'الكلمات الفريدة',
    labelEn: 'Unique Words',
    route: uniqueWordsRoutePath('tashkeel'),
    resumePath: `${WORDS_ROUTE_PATH}/unique`,
    group: 'primary',
  },
  { key: 'words-roots', labelAr: 'الجذور', labelEn: 'Roots', route: rootsRoutePath(), group: 'primary' },
  { key: 'words-lemmas', labelAr: 'الصيغ المعجمية', labelEn: 'Lemmas', route: lemmasRoutePath(), group: 'primary' },
  { key: 'words-stems', labelAr: 'الأصول الصرفية', labelEn: 'Stems', route: stemsRoutePath(), group: 'primary' },
  { key: 'words-types', labelAr: 'أنواع الكلمات', labelEn: 'Word Types', route: wordTypesRoutePath(), group: 'primary' },
];
