import { describe, expect, it } from 'vitest';

import { SourceOption } from '../models/mushaf.models';
import {
  filterLanguageGroups,
  filterSourceOptions,
  findSourceOption,
  groupSourceOptionsByLanguage,
} from './study-source-catalog.groups';

const arabicTafsir: SourceOption = {
  key: 'ar-muyassar',
  label: 'التفسير الميسر',
  languageCode: 'ar',
  languageNameAr: 'العربية',
};

const englishTranslation: SourceOption = {
  key: 'en-sahih',
  label: 'صحيح الدولية',
  languageCode: 'en',
  languageNameAr: 'الإنجليزية',
};

const englishHaleem: SourceOption = {
  key: 'en-haleem',
  label: 'هيليم',
  languageCode: 'en',
  languageNameAr: 'الإنجليزية',
};

describe('groupSourceOptionsByLanguage', () => {
  it('groups options by language code', () => {
    const groups = groupSourceOptionsByLanguage([arabicTafsir, englishTranslation, englishHaleem]);

    expect(groups).toHaveLength(2);
    expect(groups[0].languageNameAr).toBe('العربية');
    expect(groups[0].options).toHaveLength(1);
    expect(groups[1].languageNameAr).toBe('الإنجليزية');
    expect(groups[1].options).toHaveLength(2);
  });
});

describe('filterLanguageGroups', () => {
  it('filters groups by Arabic language name', () => {
    const groups = groupSourceOptionsByLanguage([arabicTafsir, englishTranslation]);

    expect(filterLanguageGroups(groups, 'إنجل')).toHaveLength(1);
    expect(filterLanguageGroups(groups, 'إنجل')[0].languageNameAr).toBe('الإنجليزية');
    expect(filterLanguageGroups(groups, 'انجل')).toHaveLength(1);
    expect(filterLanguageGroups(groups, '')).toHaveLength(2);
  });
});

describe('filterSourceOptions', () => {
  it('filters options by label with hamza-insensitive search', () => {
    const options = [arabicTafsir, englishTranslation, englishHaleem];

    expect(filterSourceOptions(options, 'صحيح')).toHaveLength(1);
    expect(filterSourceOptions(options, 'هيليم')).toHaveLength(1);
    expect(filterSourceOptions(options, '')).toHaveLength(3);
  });
});

describe('findSourceOption', () => {
  it('returns the matching option by key', () => {
    expect(findSourceOption([arabicTafsir], 'ar-muyassar')).toEqual(arabicTafsir);
    expect(findSourceOption([arabicTafsir], 'missing')).toBeUndefined();
    expect(findSourceOption([arabicTafsir], null)).toBeUndefined();
  });
});
