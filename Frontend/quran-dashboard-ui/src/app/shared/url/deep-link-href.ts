export interface DeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null | undefined>;
}

export function deepLinkToHref(target: DeepLinkTarget): string {
  const query = Object.entries(target.queryParams)
    .filter((entry): entry is [string, string] => {
      const value = entry[1];
      return value !== null && value !== undefined;
    })
    .map(([key, value]) => `${key}=${serializeQueryValue(value)}`)
    .join('&');

  return query ? `${target.path}?${query}` : target.path;
}

function serializeQueryValue(value: string): string {
  return encodeURIComponent(value).replace(/%3A/gi, ':');
}
