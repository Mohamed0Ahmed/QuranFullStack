const arabicPluralRules = new Intl.PluralRules('ar');

export function phraseOccurrenceLabel(value: number): string {
  const category = arabicPluralRules.select(value);
  if (category === 'one') {
    return 'موضع واحد';
  }
  if (category === 'two') {
    return 'موضعان';
  }
  if (category === 'few') {
    return `${value} مواضع`;
  }
  if (category === 'many') {
    return `${value} موضعًا`;
  }
  return `${value} موضع`;
}

export function phraseOptionLabel(value: number): string {
  const category = arabicPluralRules.select(value);
  if (category === 'one') {
    return 'خيار واحد';
  }
  if (category === 'two') {
    return 'خياران';
  }
  if (category === 'few') {
    return `${value} خيارات`;
  }
  if (category === 'many') {
    return `${value} خيارًا`;
  }
  return `${value} خيار`;
}
