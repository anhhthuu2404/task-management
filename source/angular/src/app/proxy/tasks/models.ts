import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { TaskPriority } from './task-priority.enum';
import type { TaskItemStatus } from './task-item-status.enum';

export interface ChecklistItemDto extends EntityDto<string> {
  taskId?: string;
  title?: string;
  isDone?: boolean;
}

export interface CommentAttachmentDto {
  fileName?: string;
  fileContent?: string;
  fileUrl?: string;
}

export interface CreateTaskCommentDto {
  text?: string;
  fileName?: string;
  fileContent?: string;
  attachments?: CommentAttachmentDto[];
}

export interface CreateTaskInputDto {
  title: string;
  description?: string;
  categoryId: string;
  assigneeId?: string;
  priority: number;
  status: number;
  dueDate?: string;
  attachments?: TaskAttachmentDto[];
}

export interface CreateUpdateChecklistItemDto {
  title: string;
}

export interface CreateUpdateSubTaskDto {
  title: string;
  assigneeId?: string;
}

export interface GetTaskListInputDto extends PagedAndSortedResultRequestDto {
  keyword?: string;
  filter?: string;
  categoryId?: string;
  assigneeId?: string;
  priority?: TaskPriority;
  status?: TaskItemStatus;
  onlyMyTasks?: boolean;
}

export interface RejectTaskInputDto {
  reason: string;
}

export interface SubTaskDto extends EntityDto<string> {
  taskId?: string;
  title?: string;
  isCompleted?: boolean;
  assigneeId?: string;
  assigneeName?: string;
}

export interface SubmitReviewInputDto {
  note?: string;
  attachments?: TaskAttachmentDto[];
}

export interface TaskActivityLogDto extends EntityDto<string> {
  taskId?: string;
  action?: string;
  userName?: string;
  details?: string;
  creationTime?: string;
}

export interface TaskAttachmentDto {
  fileName?: string;
  fileContent?: string;
  filePath?: string;
  fileUrl?: string;
}

export interface TaskCommentDto extends EntityDto<string> {
  taskId?: string;
  text?: string;
  fileUrl?: string;
  fileName?: string;
  creatorId?: string;
  creatorName?: string;
  creationTime?: string;
  attachments?: CommentAttachmentDto[];
}

export interface TaskDetailDto extends TaskDto {
  subTasks?: SubTaskDto[];
  checklistItems?: ChecklistItemDto[];
  activityLogs?: TaskActivityLogDto[];
  submittedAt?: string;
  comments?: TaskCommentDto[];
}

export interface TaskDto extends AuditedEntityDto<string> {
  title?: string;
  description?: string;
  priority?: TaskPriority;
  status?: TaskItemStatus;
  dueDate?: string;
  categoryId?: string;
  assigneeId?: string;
  assigneeName?: string;
  assigneeUserName?: string;
  fileName?: string;
  fileUrl?: string;
  progressPercent?: number;
  submissionNote?: string;
  submissionFiles?: TaskFileDto[];
}

export interface TaskFileDto {
  fileName?: string;
  fileUrl?: string;
  fileContent?: string;
}

export interface UpdateTaskCommentDto {
  text?: string;
  attachments?: CommentAttachmentDto[];
}

export interface UpdateTaskInputDto {
  title: string;
  description?: string;
  categoryId: string;
  assigneeId?: string;
  priority: number;
  status: number;
  dueDate?: string;
  attachments?: TaskAttachmentDto[];
}
