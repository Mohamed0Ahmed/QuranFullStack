import { HttpHeaders } from '@angular/common/http';

// The header is attached only when a validator is held: sending `If-None-Match: null` would be a
// malformed conditional request rather than an unconditional one.
export function conditionalHeaders(etag: string | null): HttpHeaders | undefined {
  return etag ? new HttpHeaders({ 'If-None-Match': etag }) : undefined;
}
