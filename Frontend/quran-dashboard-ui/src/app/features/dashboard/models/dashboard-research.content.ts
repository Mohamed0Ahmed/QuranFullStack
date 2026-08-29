import { NAV_MENU } from '../../../core/navigation/nav-menu';
import type { NavItem } from '../../../core/navigation/nav-items';
import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
  WORDS_ROUTE_PATH,
  lemmasRoutePath,
  phraseSearchRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from '../../../core/navigation/route-paths';

import type {
  DashboardAbwabContent,
  DashboardLinkingContent,
  DashboardMushafContent,
  DashboardPhraseContent,
  DashboardWordContent,
} from './dashboard-home.models';

function findNavigationItem(items: readonly NavItem[], key: string): NavItem | undefined {
  for (const item of items) {
    if (item.key === key) {
      return item;
    }
    const nested = findNavigationItem(item.children ?? [], key);
    if (nested !== undefined) {
      return nested;
    }
  }
  return undefined;
}

function requireNavigationRoute(key: string): string {
  const item = findNavigationItem(NAV_MENU, key);
  if (item === undefined) {
    throw new Error(`Dashboard requires the ${key} navigation route.`);
  }
  return item.route;
}

export const DASHBOARD_MUSHAF_CONTENT: DashboardMushafContent = {
  heading: 'اختر الآية، ثم افتح سياق دراستها',
  description:
    'انتقل بين صفحات المصحف، اختر آية أو كلمة، وافتح التحليل، الأبواب، التفاسير والترجمات، المتشابهات، والآيات القريبة دون فقد موضعك.',
  tabs: [
    {
      key: 'analysis',
      label: 'التحليل',
      description: 'افحص بنية الكلمة وبياناتها المرتبطة من موضع الآية نفسه.',
    },
    {
      key: 'abwab',
      label: 'الأبواب',
      description: 'راجع الأبواب المرتبطة بالآية أو أضف موضعها إلى عملية الربط.',
    },
    {
      key: 'sources',
      label: 'التفاسير والترجمات',
      description: 'بدّل بين المصادر المتاحة مع بقاء الآية المحددة أمامك.',
    },
    {
      key: 'similarity',
      label: 'المتشابهات',
      description: 'قارن المتشابهات والآيات القريبة وحدّد الكلمات والمواضع المطلوبة.',
    },
  ],
  actionLabel: 'افتح المصحف',
};

export const DASHBOARD_WORD_CONTENT: DashboardWordContent = {
  heading: 'من الجذر إلى الكلمة، ومن الكلمة إلى مواضعها',
  description:
    'تتبّع البناء الصرفي من الجذر إلى الصيغة المعجمية، ثم الأصل الصرفي والكلمة، أو افحص الكلمات بحسب نوعها النحوي.',
  sequence: [
    { label: 'الجذور', description: 'ابدأ من الأصل الجامع.', route: rootsRoutePath() },
    { label: 'الصيغ المعجمية', description: 'راجع الصيغة المعجمية.', route: lemmasRoutePath() },
    { label: 'الأصول الصرفية', description: 'انتقل إلى الأصل الصرفي.', route: stemsRoutePath() },
    { label: 'الكلمات', description: 'اعرض الكلمات ومواضعها.', route: uniqueWordsRoutePath('tashkeel') },
  ],
  overviewAction: { label: 'نظرة عامة', route: WORDS_ROUTE_PATH },
  typesAction: {
    label: 'أنواع الكلمات',
    description: 'استكشف الكلمات بحسب نوعها النحوي.',
    route: wordTypesRoutePath(),
  },
};

export const DASHBOARD_PHRASE_CONTENT: DashboardPhraseContent = {
  heading: 'ابحث في العبارة، لا في الكلمة وحدها',
  description:
    'استعرض العبارات المتكررة، كوّن سياقًا من كلمات سابقة ولاحقة، وقارن المواضع المتشابهة مع الحفاظ على حدود الكلمات الأصلية.',
  tabs: [
    {
      key: 'repetitions',
      label: 'التكرارات',
      description: 'رتّب العبارات بحسب تكرارها وافتح جميع الآيات التي وردت فيها.',
      tokens: ['عبارة', 'تكرار', 'مواضع'],
      route: phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT),
    },
    {
      key: 'context',
      label: 'البحث اليدوي',
      description: 'كوّن مسارًا من كلمات سابقة ولاحقة، ثم صفِّ الآيات وحدّد مواضع الربط.',
      tokens: ['كلمة سابقة', 'العبارة', 'كلمة لاحقة'],
      route: phraseSearchRoutePath(WORDS_PHRASES_CONTEXT_SEGMENT),
    },
    {
      key: 'similarity',
      label: 'المتشابهات',
      description: 'قارن المقطع المتطابق والاختلافات، ثم حدّد الآيات والكلمات المطلوبة.',
      tokens: ['مقطع متطابق', 'اختلاف', 'سياق'],
      route: phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT),
    },
  ],
};

export const DASHBOARD_LINKING_CONTENT: DashboardLinkingContent = {
  heading: 'اجمع المواضع، ثم حدّد شكل العلاقة',
  description:
    'حدّد الآيات والكلمات المطلوبة، اربط كل آية بصورة مستقلة أو كوحدة واحدة، أو أضف المصادر إلى مساحة الربط لمراجعتها قبل التنفيذ.',
  stages: [
    { label: 'مصادر مختارة', description: 'ابدأ بالآيات والكلمات المحددة في صفحة البحث.' },
    { label: 'مساحة الربط', description: 'اجمع أكثر من مصدر عندما تحتاج إلى مراجعة مشتركة.' },
    { label: 'مراجعة التحديد', description: 'راجع الآيات والكلمات وشكل الربط قبل التنفيذ.' },
    { label: 'تنفيذ الربط', description: 'نفّذ الربط المستقل أو الربط كوحدة واحدة.' },
  ],
  mushafActionLabel: 'ابدأ من المصحف',
  similarityAction: {
    label: 'ابدأ من متشابهات العبارات',
    route: phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT),
  },
};

export const DASHBOARD_ABWAB_CONTENT: DashboardAbwabContent = {
  heading: 'حوّل نتائج البحث إلى بناء واضح',
  description:
    'نظّم أبواب المنهج داخل شجرة مترابطة، راجع المصادر والعلاقات والمواضع، واستخدم القوالب عندما تحتاج إلى هيكل متكرر.',
  branchLabel: 'باب رئيسي',
  childLabels: ['باب فرعي', 'مصادر مرتبطة', 'علاقات ومواضع'],
  abwabAction: { label: 'افتح الأبواب', route: requireNavigationRoute('abwab-home') },
  templatesAction: {
    label: 'عرض القوالب',
    route: requireNavigationRoute('abwab-templates'),
  },
};
