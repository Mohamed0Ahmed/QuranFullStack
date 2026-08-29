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

export function phraseWordLabel(value: number): string {
  const category = arabicPluralRules.select(value);
  if (category === 'one') {
    return 'كلمة واحدة';
  }
  if (category === 'two') {
    return 'كلمتان';
  }
  if (category === 'few') {
    return `${value} كلمات`;
  }
  return `${value} كلمة`;
}
