import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUserDto } from './current-user.model';

/**
 * API boundary for the backend `Access/` bounded context (Feature 033).
 *
 * Thin by contract (mirrors `core/data-access/system.api.ts`): it types the call and
 * returns the raw `ApiResponse<T>` envelope. Unwrapping `isSuccess` / `data` / `message`
 * and mapping failures is the store's job (`current-user.store.ts`), per
 * `.architecture/API_INTEGRATION_GUIDELINES.md`.
 */
@Injectable({ providedIn: 'root' })
export class AccessApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  /** The authenticated user's local account (auto-provisioned on first login). */
  getMe(): Observable<ApiResponse<CurrentUserDto>> {
    return this.http.get<ApiResponse<CurrentUserDto>>(`${this.baseUrl}/api/access/me`);
  }
}
