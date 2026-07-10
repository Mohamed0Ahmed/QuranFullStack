import { SourceOption } from '../models/mushaf.models';
import { arabicSearchIncludes } from './arabic-search-normalize';

export interface LanguageSourceGroup {
  languageCode: string;
  languageNameAr: string;
  options: SourceOption[];
}

function languageGroupKey(option: SourceOption): string {
  return option.languageCode ?? option.languageNameAr ?? option.key;
}

export function groupSourceOptionsByLanguage(options: SourceOption[]): LanguageSourceGroup[] {
  const groups: LanguageSourceGroup[] = [];
  const groupIndex = new Map<string, number>();

  for (const option of options) {
    const key = languageGroupKey(option);
    let index = groupIndex.get(key);
    if (index === undefined) {
      index = groups.length;
      groupIndex.set(key, index);
      groups.push({
        languageCode: option.languageCode ?? key,
        languageNameAr: option.languageNameAr ?? option.languageCode ?? '—',
        options: [],
      });
    }
    groups[index].options.push(option);
  }

  return groups;
}

export function filterLanguageGroups(
  groups: LanguageSourceGroup[],
  query: string,
): LanguageSourceGroup[] {
  if (!query.trim()) {
    return groups;
  }

  return groups.filter((group) => arabicSearchIncludes(group.languageNameAr, query));
}

export function filterSourceOptions(options: SourceOption[], query: string): SourceOption[] {
  if (!query.trim()) {
    return options;
  }

  return options.filter((option) => arabicSearchIncludes(option.label, query));
}

export function findSourceOption(
  options: SourceOption[],
  key: string | null | undefined,
): SourceOption | undefined {
  if (!key) {
    return undefined;
  }

  return options.find((option) => option.key === key);
}
