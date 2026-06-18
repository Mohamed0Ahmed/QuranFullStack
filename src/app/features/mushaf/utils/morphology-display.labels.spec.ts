import { describe, expect, it } from 'vitest';

import { morphologyFeatureLabelAr } from './morphology-display.labels';

describe('morphologyFeatureLabelAr', () => {
  it('maps known API morphology values to Arabic labels', () => {
    expect(morphologyFeatureLabelAr('past')).toBe('ماضٍ');
    expect(morphologyFeatureLabelAr('present')).toBe('مضارع');
    expect(morphologyFeatureLabelAr('imperative')).toBe('أمر');
    expect(morphologyFeatureLabelAr('active')).toBe('معلوم');
    expect(morphologyFeatureLabelAr('passive')).toBe('مجهول');
    expect(morphologyFeatureLabelAr('nominative')).toBe('مرفوع');
    expect(morphologyFeatureLabelAr('accusative')).toBe('منصوب');
    expect(morphologyFeatureLabelAr('genitive')).toBe('مجرور');
  });

  it('returns an em dash for unknown or empty values', () => {
    expect(morphologyFeatureLabelAr(null)).toBe('—');
    expect(morphologyFeatureLabelAr(undefined)).toBe('—');
    expect(morphologyFeatureLabelAr('unknown-value')).toBe('—');
  });
});
