import { describe, expect, it } from 'vitest';

import { SourceOption, StudySourceCatalogItemDto } from '../models/mushaf.models';
import {
  fullI3rabCatalogItemToOption,
  tafsirCatalogItemToOption,
  translationCatalogItemToOption,
} from './study-source-catalog.labels';

function tafsirItem(overrides: Partial<StudySourceCatalogItemDto> = {}): StudySourceCatalogItemDto {
  return {
    sourceKey: 'ar-muyassar',
    displayNameAr: 'التفسير الميسر',
    displayNameEn: 'Muyassar',
    languageCode: 'ar',
    languageNameAr: 'العربية',
    direction: 'rtl',
    tafsirKind: 'brief',
    translationType: null,
    ...overrides,
  };
}

describe('study-source-catalog.labels', () => {
  it('maps tafsir detailed kind with suffix', () => {
    const option = tafsirCatalogItemToOption(tafsirItem({ tafsirKind: 'detailed' }));
    expect(option.label).toBe('التفسير الميسر (مفصّل)');
  });

  it('maps translation with footnotes suffix', () => {
    const option = translationCatalogItemToOption(
      tafsirItem({
        sourceKey: 'en-fn',
        displayNameAr: 'ترجمة',
        translationType: 'with_footnotes',
        tafsirKind: null,
      }),
    );
    expect(option.label).toBe('ترجمة (بملاحظات)');
  });

  it('maps full i3rab without suffix', () => {
    const option: SourceOption = fullI3rabCatalogItemToOption(
      tafsirItem({ sourceKey: 'muyassar', displayNameAr: 'الإعراب الميسر', tafsirKind: null }),
    );
    expect(option.label).toBe('الإعراب الميسر');
  });
});
