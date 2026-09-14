import type { TaskReportItemDto, TaskReportQueryDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getTaskReport = (input: TaskReportQueryDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskReportItemDto[]>({
      method: 'GET',
      url: '/api/app/report/get-task-report',
      params: { employeeId: input.employeeId, departmentId: input.departmentId, projectId: input.projectId, fromDate: input.fromDate, toDate: input.toDate },
    },
    { apiName: this.apiName,...config });
}