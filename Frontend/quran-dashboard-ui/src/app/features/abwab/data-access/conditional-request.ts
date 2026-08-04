import { HttpHeaders } from '@angular/common/http';

export function conditionalHeaders(etag: string | null): HttpHeaders | undefined {
  return etag ? new HttpHeaders({ 'If-None-Match': etag }) : undefined;
}
