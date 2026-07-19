import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUserDto } from './current-user.model';

// Thin API boundary for the backend `Access/` context (Feature 033): types the call and returns
// the raw `ApiResponse<T>` envelope for the store to unwrap. Deliberately does NOT mirror
// `data-access/system.api.ts` — SystemApi is a documented exception that unwraps, throws, and
// caches; AccessApi keeps the guideline's default shape.
@Injectable({ providedIn: 'root' })
export class AccessApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getMe(): Observable<ApiResponse<CurrentUserDto>> {
    return this.http.get<ApiResponse<CurrentUserDto>>(`${this.baseUrl}/api/access/me`);
  }
}
