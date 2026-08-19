import type { AssignUserToDepartmentDto, CreateUpdateDepartmentDto, DepartmentDto, DepartmentTreeDto, GetDepartmentListDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DepartmentService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  assignUser = (input: AssignUserToDepartmentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/department/assign-user',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateDepartmentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'POST',
      url: '/api/app/department',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/department/${id}`,
    },
    { apiName: this.apiName,...config });
  

  deleteUser = (departmentId: string, userId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: '/api/app/department/user',
      params: { departmentId, userId },
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'GET',
      url: `/api/app/department/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetDepartmentListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<DepartmentDto>>({
      method: 'GET',
      url: '/api/app/department',
      params: { filter: input.filter, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getTree = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentTreeDto[]>({
      method: 'GET',
      url: '/api/app/department/tree',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateDepartmentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'PUT',
      url: `/api/app/department/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}