import type { DashboardStatisticsDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getStatistics = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DashboardStatisticsDto>({
      method: 'GET',
      url: '/api/app/dashboard/statistics',
    },
    { apiName: this.apiName,...config });
}