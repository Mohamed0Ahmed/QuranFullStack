const TRAILING_SPACE_BEFORE_QURANIC_MARK = /\s+([\u06D6-\u06ED])$/u;

export function toQuranWordDisplayText(textUthmani: string): string {
  return textUthmani.replace(TRAILING_SPACE_BEFORE_QURANIC_MARK, '$1');
}
