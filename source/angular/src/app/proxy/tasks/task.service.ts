import type { ChecklistItemDto, CreateTaskCommentDto, CreateTaskInputDto, CreateUpdateChecklistItemDto, CreateUpdateSubTaskDto, GetTaskListInputDto, SubTaskDto, TaskCommentDto, TaskDetailDto, TaskDto, UpdateTaskCommentDto, UpdateTaskInputDto } from './models';
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
  

  createChecklistItem = (taskId: string, input: CreateUpdateChecklistItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ChecklistItemDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/checklist-item`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createComment = (taskId: string, input: CreateTaskCommentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskCommentDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/comment`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createSubTask = (taskId: string, input: CreateUpdateSubTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SubTaskDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/sub-task`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  deleteChecklistItem = (itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/checklist-item/${itemId}`,
    },
    { apiName: this.apiName,...config });
  

  deleteComment = (commentId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/comment/${commentId}`,
    },
    { apiName: this.apiName,...config });
  

  deleteSubTask = (subTaskId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/sub-task/${subTaskId}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'GET',
      url: `/api/app/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getComments = (taskId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskCommentDto[]>({
      method: 'GET',
      url: `/api/app/task/${taskId}/comments`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTaskListInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TaskDto>>({
      method: 'GET',
      url: '/api/app/task',
      params: { keyword: input.keyword, filter: input.filter, categoryId: input.categoryId, assigneeId: input.assigneeId, priority: input.priority, status: input.status, onlyMyTasks: input.onlyMyTasks, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getTaskDetail = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDetailDto>({
      method: 'GET',
      url: `/api/app/task/${id}/detail`,
    },
    { apiName: this.apiName,...config });
  

  toggleChecklistItemStatus = (itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: `/api/app/task/checklist-item/${itemId}/toggle`,
    },
    { apiName: this.apiName,...config });
  

  toggleSubTaskStatus = (subTaskId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: `/api/app/task/sub-task/${subTaskId}/toggle`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateTaskInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/app/task/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateAssignee = (id: string, assigneeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: `/api/app/task/${id}/assignee`,
      params: { assigneeId },
    },
    { apiName: this.apiName,...config });
  

  updateChecklistItem = (itemId: string, input: CreateUpdateChecklistItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ChecklistItemDto>({
      method: 'PUT',
      url: `/api/app/task/checklist-item/${itemId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateComment = (commentId: string, input: UpdateTaskCommentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskCommentDto>({
      method: 'PUT',
      url: `/api/app/task/comment/${commentId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateStatus = (id: string, status: TaskItemStatus, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: `/api/app/task/${id}/status`,
      params: { status },
    },
    { apiName: this.apiName,...config });
  

  updateSubTask = (subTaskId: string, input: CreateUpdateSubTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SubTaskDto>({
      method: 'PUT',
      url: `/api/app/task/sub-task/${subTaskId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}