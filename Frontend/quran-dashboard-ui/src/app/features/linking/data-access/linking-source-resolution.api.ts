import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingResolvedSourceDto } from '../../../core/api/generated/models/linking-resolved-source-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { toLinkingSourceDescriptorBody } from '../utils/linking-source-descriptor-body';

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolutionApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  resolveSource(source: LinkingSourceDescriptor): Observable<ApiResponse<LinkingResolvedSourceDto>> {
    return this.http.post<ApiResponse<LinkingResolvedSourceDto>>(
      `${this.baseUrl}/api/linking/sources/resolve`,
      toLinkingSourceDescriptorBody(source),
    );
  }
}
