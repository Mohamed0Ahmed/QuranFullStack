import { HttpHeaders } from '@angular/common/http';

export function phraseSearchConditionalHeaders(
  etag: string | null,
): HttpHeaders | undefined {
  return etag ? new HttpHeaders({ 'If-None-Match': etag }) : undefined;
}
