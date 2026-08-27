import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RestService } from '@abp/ng.core';

// --- ENUMS ---
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

// --- FILE & ATTACHMENT DTOS ---
export interface TaskFileDto {
  id?: string;
  fileName?: string;
  name?: string;
  fileUrl?: string;
  url?: string;
  path?: string;
}

export interface CommentAttachmentDto {
  fileName: string;
  fileContent?: string;
  fileUrl?: string;
}

// --- TASK DTOS ---
export interface TaskDto {
  id: string;
  title: string;
  assigneeId?: string; // Bổ sung an toàn để quản lý ID người thực hiện
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

// === BỔNG SUNG: TASK HISTORY DTO (Lịch sử thay đổi gán người thực hiện) ===
export interface TaskHistoryDto {
  id?: string;
  taskId?: string;
  action?: string;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  oldAssigneeId?: string;
  oldAssigneeName?: string;
  newAssigneeId?: string;
  newAssigneeName?: string;
  creatorId?: string;
  creatorName?: string;
  creationTime?: string;
}

// --- COMMENT DTOS ---
export interface TaskCommentDto {
  id: string;
  taskId: string;
  text: string;
  fileUrl?: string;
  fileName?: string;
  creatorId?: string;
  creatorName?: string;
  creationTime: string;
  attachments: CommentAttachmentDto[];
}

export interface CreateTaskCommentDto {
  text: string;
  fileName?: string;
  fileContent?: string;
  attachments?: CommentAttachmentDto[];
}

export interface UpdateTaskCommentDto {
  text: string;
  attachments?: CommentAttachmentDto[];
}

// --- ACTIVITY LOG DTO ---
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
  comments?: TaskCommentDto[];
  activityLogs?: ActivityLogDto[];
  histories?: TaskHistoryDto[]; // <-- Tích hợp danh sách lịch sử an toàn tuyệt đối
}

// --- INPUT DTOS ---
export interface SubmitReportInput {
  note: string;
  attachments: { fileName: string; fileContent: string }[];
}

export interface RejectTaskInput {
  reason: string;
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

  // --- TASK API ---
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
      url: `/api/app/task/${id}/detail`
    }, { apiName: this.apiName });
  }

  deleteTask(id: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'DELETE',
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

  // --- SUBMISSION MANAGEMENT API ---
  updateSubmission(id: string, input: SubmitReportInput): Observable<void> {
    return this.restService.request<SubmitReportInput, void>({
      method: 'PUT',
      url: `/api/app/task/${id}/submission`,
      body: input
    }, { apiName: this.apiName });
  }

  deleteSubmission(id: string): Observable<void> {
    return this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/${id}/submission`
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

  // --- CẬP NHẬT LỊCH TRÌNH (CHO CALENDAR VIEW) ---
  updateSchedule(id: string, dueDate: string | null): Observable<void> {
    return this.restService.request<{ dueDate: string | null }, void>({
      method: 'PUT',
      url: `/api/app/task/${id}/schedule`,
      body: { dueDate }
    }, { apiName: this.apiName });
  }

  // --- BỔ SUNG: API GÁN/ĐỔI NGƯỜI THỰC HIỆN (ASSIGN/RE-ASSIGN) ---
  updateAssignee(id: string, assigneeId: string | null): Observable<TaskDto> {
    return this.restService.request<{ assigneeId: string | null }, TaskDto>({
      method: 'POST', // Hoặc PUT tùy thuộc vào route API Backend của bạn
      url: `/api/app/task/${id}/assignee`,
      body: { assigneeId }
    }, { apiName: this.apiName });
  }

  // --- COMMENTS API ---
  createComment(taskId: string, input: CreateTaskCommentDto): Observable<TaskCommentDto> {
    return this.restService.request<CreateTaskCommentDto, TaskCommentDto>({
      method: 'POST',
      url: `/api/app/task/${taskId}/comment`,
      body: input
    }, { apiName: this.apiName });
  }

  updateComment(commentId: string, input: UpdateTaskCommentDto): Observable<void> {
    return this.restService.request<UpdateTaskCommentDto, void>({
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

  // --- SUBTASKS API ---
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

  // --- CHECKLIST ITEMS API ---
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

  getTaskTimeline(taskId: string): Observable<any[]> {
    return this.restService.request<any, any[]>({
      method: 'GET',
      url: `/api/app/task/${taskId}/timeline`
    }, { apiName: this.apiName });
  }
}