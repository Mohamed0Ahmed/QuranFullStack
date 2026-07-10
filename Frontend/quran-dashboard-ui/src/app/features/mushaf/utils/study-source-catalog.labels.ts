import { SourceOption, StudySourceCatalogItemDto } from '../models/mushaf.models';

function withSuffix(label: string, suffix: string, active: boolean): string {
  return active ? `${label} ${suffix}` : label;
}

export function tafsirCatalogItemToOption(item: StudySourceCatalogItemDto): SourceOption {
  return {
    key: item.sourceKey,
    label: withSuffix(item.displayNameAr, '(مفصّل)', item.tafsirKind === 'detailed'),
    languageCode: item.languageCode,
    languageNameAr: item.languageNameAr,
  };
}

export function translationCatalogItemToOption(item: StudySourceCatalogItemDto): SourceOption {
  return {
    key: item.sourceKey,
    label: withSuffix(item.displayNameAr, '(بملاحظات)', item.translationType === 'with_footnotes'),
    languageCode: item.languageCode,
    languageNameAr: item.languageNameAr,
  };
}

export function fullI3rabCatalogItemToOption(item: StudySourceCatalogItemDto): SourceOption {
  return {
    key: item.sourceKey,
    label: item.displayNameAr,
    languageCode: item.languageCode,
    languageNameAr: item.languageNameAr,
  };
}
