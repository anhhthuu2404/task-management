import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CustomUserManagementService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  changePassword = (userId: string, newPassword: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/custom-user/change-password',
      params: { userId, newPassword },
    },
    { apiName: this.apiName,...config });
}