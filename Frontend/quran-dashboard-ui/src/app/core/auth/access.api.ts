import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUserDto } from './current-user.model';

@Injectable({ providedIn: 'root' })
export class AccessApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getMe(): Observable<ApiResponse<CurrentUserDto>> {
    return this.http.get<ApiResponse<CurrentUserDto>>(`${this.baseUrl}/api/access/me`);
  }
}
