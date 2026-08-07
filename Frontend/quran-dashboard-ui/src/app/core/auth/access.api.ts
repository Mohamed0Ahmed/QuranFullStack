import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUserResponse } from '../api/generated/models/current-user-response';

@Injectable({ providedIn: 'root' })
export class AccessApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getMe(): Observable<ApiResponse<CurrentUserResponse>> {
    return this.http.get<ApiResponse<CurrentUserResponse>>(`${this.baseUrl}/api/access/me`);
  }
}
