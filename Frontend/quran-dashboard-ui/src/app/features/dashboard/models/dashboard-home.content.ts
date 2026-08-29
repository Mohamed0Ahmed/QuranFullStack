import { NAV_ITEMS, type NavItem } from '../../../core/navigation/nav-items';
import { phraseSearchRoutePath } from '../../../core/navigation/route-paths';

import type {
  DashboardHeroDefinition,
  DashboardNavigationDefinition,
  DashboardWorkflowStep,
} from './dashboard-home.models';

function requireNavigationItem(key: string): NavItem {
  const item = NAV_ITEMS.find((candidate) => candidate.key === key);
  if (item === undefined) {
    throw new Error(`Dashboard requires the ${key} navigation item.`);
  }
  return item;
}

const mushafNavigationItem = requireNavigationItem('mushaf');
const wordsNavigationItem = requireNavigationItem('words');
const abwabNavigationItem = requireNavigationItem('abwab');
const quranSearchNavigationItem: NavItem = {
  ...requireNavigationItem('quran-search'),
  route: phraseSearchRoutePath(),
};

export const DASHBOARD_HERO: DashboardHeroDefinition = {
  content: {
    sectionKey: 'hero',
    title: 'مساحة العمل القرآنية',
    ayah: '﴿ وَنَزَّلْنَا عَلَيْكَ الْكِتَابَ تِبْيَانًا لِكُلِّ شَيْءٍ ﴾',
    description:
      'اقرأ الآية في موضعها، تتبّع ألفاظها وسياقاتها، ثم نظّم نتائج البحث داخل أبواب المنهج.',
    mushafActionLabel: 'افتح قارئ المصحف',
    workflowActionLabel: 'تعرّف على مسار العمل',
    searchAction: {
      label: 'ابدأ البحث في القرآن',
      route: phraseSearchRoutePath(),
    },
  },
  mushafNavigationItem,
};

export const DASHBOARD_RESUME_ITEMS: readonly DashboardNavigationDefinition[] = [
  { label: 'المصحف', navigationItem: mushafNavigationItem },
  { label: 'البحث في القرآن', navigationItem: quranSearchNavigationItem },
  { label: 'الكلمات والجذور', navigationItem: wordsNavigationItem },
  { label: 'الأبواب', navigationItem: abwabNavigationItem },
];

export const DASHBOARD_WORKFLOW_STEPS: readonly DashboardWorkflowStep[] = [
  { key: 'mushaf', label: 'اقرأ في المصحف' },
  { key: 'words', label: 'افحص الكلمة والعبارة' },
  { key: 'phrases', label: 'استكشف السياق' },
  { key: 'linking', label: 'حدّد واربط' },
  { key: 'abwab', label: 'نظّم داخل الأبواب' },
];

export const DASHBOARD_ENTRY_ITEMS: readonly DashboardNavigationDefinition[] = [
  {
    label: 'قراءة آية ودراسة سياقها',
    description: 'ابدأ من موضعك الأخير في قارئ المصحف.',
    navigationItem: mushafNavigationItem,
  },
  {
    label: 'البحث عن كلمة أو عبارة',
    description: 'انتقل إلى أدوات البحث في العبارات والسياقات.',
    navigationItem: quranSearchNavigationItem,
  },
  {
    label: 'مراجعة بناء المنهج',
    description: 'تابع تنظيم الأبواب والمصادر والعلاقات.',
    navigationItem: abwabNavigationItem,
  },
];

export const DASHBOARD_ENTRY_HEADING = 'اختر نقطة البداية';
