import {
  WORDS_ROUTE_PATH,
  lemmasRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from './route-paths';

export interface WordsNavItem {
  labelAr: string;
  route: string;
}

export const WORDS_MENU_ITEMS: readonly WordsNavItem[] = [
  { labelAr: 'الرئيسية', route: WORDS_ROUTE_PATH },
  { labelAr: 'الكلمات الفريدة', route: uniqueWordsRoutePath('tashkeel') },
  { labelAr: 'الجذور', route: rootsRoutePath() },
  { labelAr: 'الصيغ المعجمية', route: lemmasRoutePath() },
  { labelAr: 'الأصول الصرفية', route: stemsRoutePath() },
  { labelAr: 'أنواع الكلمات', route: wordTypesRoutePath() },
];
