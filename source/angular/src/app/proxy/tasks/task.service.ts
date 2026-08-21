import type { CreateTaskInputDto, GetTaskListInputDto, TaskDto, UpdateTaskInputDto } from './models';
import type { TaskItemStatus } from './task-item-status.enum';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateTaskInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: '/api/app/task',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'GET',
      url: `/api/app/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTaskListInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TaskDto>>({
      method: 'GET',
      url: '/api/app/task',
      params: { keyword: input.keyword, categoryId: input.categoryId, assigneeId: input.assigneeId, priority: input.priority, status: input.status, onlyMyTasks: input.onlyMyTasks, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateTaskInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/app/task/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateStatus = (id: string, status: TaskItemStatus, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/app/task/${id}/status`,
      params: { status },
    },
    { apiName: this.apiName,...config });
}