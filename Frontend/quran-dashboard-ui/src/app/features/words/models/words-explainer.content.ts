/**
 * Words-explainer content (Feature 031) — the single approved copy source that feeds BOTH
 * the per-page explorer heroes AND the hub navigation cards, so a card description and its
 * page's tagline can never drift.
 *
 * Copy is transcribed verbatim from the approved design comp. Presentation-only:
 * no backend read, no count, no Quran-data logic. The example words rendered per page are clearly
 * labelled `مثال` illustrative morphology — never presented as Quranic quotations or queryable
 * counts (plan D10).
 *
 * Consumers read these records through TDZ-safe getters, never `readonly` field initialisers
 * (`words/README.md`: label/content consts resolve to `undefined` in the bundled test build
 * otherwise).
 */

export type WordsExplainerKey = 'unique' | 'roots' | 'lemmas' | 'stems' | 'word-types';

export interface WordsExplainerContent {
  /** Stable slug — drives testids and the collapse-memory storage key (never the Arabic label). */
  readonly key: WordsExplainerKey;
  readonly ordinal: string;
  readonly eyebrow: string;
  /** MUST equal the explorer page's own `pageTitle` (asserted in the content spec). */
  readonly title: string;
  /** The one-liner — shown under the title AND as the hub card's description. */
  readonly tagline: string;
  readonly body: string;
  readonly benefit: string;
}

/** Canonical order (widest morphological grouping → narrowest, then the grammatical axis). */
export const WORDS_EXPLAINER_ORDER: readonly WordsExplainerKey[] = [
  'unique',
  'roots',
  'lemmas',
  'stems',
  'word-types',
];

export const WORDS_EXPLAINER_CONTENT: Readonly<Record<WordsExplainerKey, WordsExplainerContent>> = {
  unique: {
    key: 'unique',
    ordinal: '٠١',
    eyebrow: 'نقطة البداية',
    title: 'الكلمات الفريدة',
    tagline: 'كل كلمة قرآنية فريدة، وأين وردت — وأين لم تَرِد.',
    body: 'تستعرض كل كلمة مميّزة في القرآن مرة واحدة، بعدد مرات ظهورها والسور والآيات التي وردت فيها (وأيضًا التي لم تُذكر فيها). تقدر تفلتر بالنوع أو الجذر، وتقفز من أي آية مباشرة إلى المصحف. وتشتغل بوضعين تختار بينهما:',
    benefit:
      'أسرع طريق للوصول لأي كلمة، والتأكد من عدد ورودها، ومراجعة تغطيتها عبر السور — بما فيها السور التي لم تُذكر فيها — للتحقق من الربط.',
  },
  roots: {
    key: 'roots',
    ordinal: '٠٢',
    eyebrow: 'أوسع تجميع صرفي',
    title: 'الجذور',
    tagline: 'النواة التي تخرج منها الأسماء والأفعال جميعًا.',
    body: 'الجذر هو النواة الصامتة (ثلاثة حروف غالبًا) التي تحمل معنى عامًّا، وتخرج منه كل الاشتقاقات — أسماءً كانت أو أفعالًا. هذا القسم يجمع لكل جذر كل ما اشتُقّ منه في القرآن كله: كلماته وصيغه وأصوله، بانتشارها في الآيات والسور.',
    benefit:
      'صورة كاملة لتفرّع الجذر: كل كلمة وصيغة وأصل يُنتِجها، وانتشاره، وفجوات غيابه — أساس البحث الموضوعي القائم على الجذور.',
  },
  lemmas: {
    key: 'lemmas',
    ordinal: '٠٣',
    eyebrow: 'الصورة المعجمية',
    title: 'الصيغ المعجمية',
    tagline: 'مدخل القاموس: صورة واحدة تجمع تصاريفها الإعرابية.',
    body: 'الصيغة المعجمية هي الصورة القاموسية للكلمة — تجمع صور الكلمة التي تختلف بالتصريف الإعرابي فقط (الحالة، العدد، التعريف) دون أن يتغيّر المعنى. كل صيغة تنتمي إلى جذر واحد، وتحتها الكلمات السطحية والأصول الصرفية.',
    benefit:
      'تعمل على مستوى مدخل القاموس: أي كلمات تُحقّق الصيغة، وأي جذر تنتمي إليه، وأي أصول تحتها — تجسر بين مستوى الجذر ومستوى الكلمة.',
  },
  stems: {
    key: 'stems',
    ordinal: '٠٤',
    eyebrow: 'الطبقة الصرفية الأدق',
    title: 'الأصول الصرفية',
    tagline: 'قلب الكلمة بعد نزع السوابق واللواحق.',
    body: 'الأصل الصرفي هو الجزء الجوهري من الكلمة بعد نزع السوابق (مثل: الـ، و، ف، بـ) واللواحق (مثل: ون، ين، ها)، وهو يقع بين الصيغة المعجمية والكلمة السطحية. هذا القسم يجمع لكل أصل كلماته وتوزيع أنواعه النحوية وارتباطه الأساسي بجذره وصيغته.',
    benefit:
      'أدقّ طبقة صرفية مع توزيع الأنواع، فترى كيف يتصرّف الأصل عبر أقسام الكلام وارتباطه الأساسي — مفيدة لتمييز المتشابهات (الكلمات المتجانسة رسمًا).',
  },
  'word-types': {
    key: 'word-types',
    ordinal: '٠٥',
    eyebrow: 'التصنيف النحوي',
    title: 'أنواع الكلمات',
    tagline: 'استعلامات نحوية حقيقية عبر القرآن كله.',
    body: 'هذا القسم لا يصنّف الكلمة حسب صرفها، بل حسب نوعها النحوي (قسمها من أقسام الكلام) وسياقها. تصفّي كلمات القرآن حسب النوع، مع الحالة والزمن والصوت، وتقرأ النطاق من أربع زوايا في آنٍ واحد: كلمات، جذور، أصول، صيغ.',
    benefit:
      'تشغّل استعلامات نحوية لا يجيبها قسم آخر، وتقرأ النطاق من أربع زوايا معًا — سطح البحث النحوي المكمّل للأقسام الصرفية الأربعة.',
  },
};

/**
 * The hub's own intro band (approved comp intro `:73-74`). The "دليل الأدمن" eyebrow is
 * intentionally dropped: the whole app is the admin dashboard (plan D8).
 */
export const WORDS_HUB_INTRO = {
  title: 'أقسام دراسة الكلمات القرآنية',
  subtitle:
    'خمسة مستكشفات، كل واحد يفحص القرآن من زاوية. السلسلة الصرفية من الأوسع للأضيق، بالإضافة إلى التصنيف النحوي.',
} as const;

/**
 * A single chain node in the hub orientation diagram (approved comp `:75-81`) — the clearest
 * single artifact explaining why there are five sections. `connector` is the separator rendered
 * AFTER this node (`arrow` `←` between the morphological layers, `plus` `+` before the grammatical
 * axis, `null` on the last node). The grammatical/morphological split is carried TEXTUALLY by the
 * `(نحوي)` marker in the last label — never by color (green-budget / Green Thread): every node
 * renders in the same neutral style.
 */
export interface WordsHubChainNode {
  readonly label: string;
  readonly connector: 'arrow' | 'plus' | null;
}

export const WORDS_HUB_CHAIN: readonly WordsHubChainNode[] = [
  { label: 'الجذر', connector: 'arrow' },
  { label: 'الصيغة المعجمية', connector: 'arrow' },
  { label: 'الأصل الصرفي', connector: 'arrow' },
  { label: 'الكلمة', connector: 'plus' },
  { label: 'أنواع الكلمات (نحوي)', connector: null },
];
