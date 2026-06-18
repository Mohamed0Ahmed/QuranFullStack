const MORPHOLOGY_FEATURE_LABELS_AR: Readonly<Record<string, string>> = {
  past: 'ماضٍ',
  present: 'مضارع',
  imperative: 'أمر',
  active: 'معلوم',
  passive: 'مجهول',
  nominative: 'مرفوع',
  accusative: 'منصوب',
  genitive: 'مجرور',
};

export function morphologyFeatureLabelAr(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  return MORPHOLOGY_FEATURE_LABELS_AR[value] ?? '—';
}
