import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RestService } from '@abp/ng.core';

export enum TaskStatus {
  New = 0,
  InProgress = 1,
  InReview = 2,
  Completed = 3,
  Cancelled = 4
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Urgent = 3
}

export interface TaskFileDto {
  id?: string;
  fileName?: string;
  name?: string;
  fileUrl?: string;
  url?: string;
  path?: string;
}

export interface TaskDto {
  id: string;
  title: string;
  assigneeName?: string;
  priority: TaskPriority;
  status: TaskStatus;
  progress?: number;
  dueDate?: string;
}

export interface SubTaskDto {
  id: string;
  title: string;
  isCompleted: boolean;
}

export interface ChecklistItemDto {
  id: string;
  title: string;
  isDone: boolean;
}

export interface CommentDto {
  id: string;
  text: string;
  creatorName?: string;
  creationTime?: string;
  attachments?: TaskFileDto[];
}

export interface ActivityLogDto {
  id?: string;
  userName?: string;
  creatorName?: string;
  action?: string;
  description?: string;
  message?: string;
  creationTime?: string;
}

export interface TaskDetailDto extends TaskDto {
  description?: string;
  submittedAt?: string;
  submissionNote?: string;
  reportNote?: string;
  note?: string;
  submissionFiles?: TaskFileDto[];
  reportFiles?: TaskFileDto[];
  attachments?: TaskFileDto[];
  files?: TaskFileDto[];
  subTasks?: SubTaskDto[];
  checklistItems?: ChecklistItemDto[];
  comments?: CommentDto[];
  activityLogs?: ActivityLogDto[];
}

export interface SubmitReportInput {
  note: string;
  attachments: { fileName: string; fileContent: string }[];
}

export interface RejectTaskInput {
  reason: string;
}

export interface CreateCommentInput {
  taskId: string;
  text: string;
  attachments?: { fileName: string; fileContent: string }[];
}

export interface UpdateSubTaskInput {
  title: string;
  isCompleted?: boolean;
}

export interface UpdateChecklistItemInput {
  title: string;
  isDone?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private apiName = 'Default';

  constructor(private restService: RestService) {}

  getTasks(params: any): Observable<{ items: TaskDto[]; totalCount: number }> {
    return this.restService.request<any, { items: TaskDto[]; totalCount: number }>({
      method: 'GET',
      url: '/api/app/task',
      params
    }, { apiName: this.apiName });
  }

  getTaskDetail(id: string): Observable<TaskDetailDto> {
    return this.restService.request<any, TaskDetailDto>({
      method: 'GET',
      url: `/api/app/task/${id}`
    }, { apiName: this.apiName });
  }

  submitForReview(id: string, input: SubmitReportInput): Observable<void> {
    return this.restService.request<SubmitReportInput, void>({
      method: 'POST',
      url: `/api/app/task/${id}/submit-for-review`,
      body: input
    }, { apiName: this.apiName });
  }

  approveTask(id: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/task/${id}/approve`
    }, { apiName: this.apiName });
  }

  rejectTask(id: string, input: RejectTaskInput): Observable<void> {
    return this.restService.request<RejectTaskInput, void>({
      method: 'POST',
      url: `/api/app/task/${id}/reject`,
      body: input
    }, { apiName: this.apiName });
  }

  updateStatus(id: string, status: TaskStatus): Observable<void> {
    return this.restService.request<any, void>({
      method: 'PUT',
      url: `/api/app/task/${id}/status`,
      params: { status }
    }, { apiName: this.apiName });
  }

  // --- COMMENTS ---
  createComment(input: CreateCommentInput): Observable<CommentDto> {
    return this.restService.request<CreateCommentInput, CommentDto>({
      method: 'POST',
      url: `/api/app/task/${input.taskId}/comment`,
      body: input
    }, { apiName: this.apiName });
  }

  addComment(taskId: string, input: any): Observable<CommentDto> {
    return this.createComment({ ...input, taskId });
  }

  updateComment(commentId: string, input: { text: string }): Observable<void> {
    return this.restService.request<{ text: string }, void>({
      method: 'PUT',
      url: `/api/app/task/comment/${commentId}`,
      body: input
    }, { apiName: this.apiName });
  }

  deleteComment(commentId: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/comment/${commentId}`
    }, { apiName: this.apiName });
  }

  // --- SUBTASKS ---
  createSubTask(taskId: string, input: { title: string }): Observable<SubTaskDto> {
    return this.restService.request<{ title: string }, SubTaskDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/sub-task`,
      body: input
    }, { apiName: this.apiName });
  }

  updateSubTask(subTaskId: string, input: UpdateSubTaskInput): Observable<void> {
    return this.restService.request<UpdateSubTaskInput, void>({
      method: 'PUT',
      url: `/api/app/task/sub-task/${subTaskId}`,
      body: input
    }, { apiName: this.apiName });
  }

  deleteSubTask(subTaskId: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/sub-task/${subTaskId}`
    }, { apiName: this.apiName });
  }

  // --- CHECKLIST ITEMS ---
  createChecklistItem(taskId: string, input: { title: string }): Observable<ChecklistItemDto> {
    return this.restService.request<{ title: string }, ChecklistItemDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/checklist-item`,
      body: input
    }, { apiName: this.apiName });
  }

  updateChecklistItem(itemId: string, input: UpdateChecklistItemInput): Observable<void> {
    return this.restService.request<UpdateChecklistItemInput, void>({
      method: 'PUT',
      url: `/api/app/task/checklist-item/${itemId}`,
      body: input
    }, { apiName: this.apiName });
  }

  deleteChecklistItem(itemId: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/checklist-item/${itemId}`
    }, { apiName: this.apiName });
  }
}