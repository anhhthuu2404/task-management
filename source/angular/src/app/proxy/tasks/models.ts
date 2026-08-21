import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { TaskPriority } from './task-priority.enum';
import type { TaskItemStatus } from './task-item-status.enum';

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

export interface GetTaskListInputDto extends PagedAndSortedResultRequestDto {
  keyword?: string;
  categoryId?: string;
  assigneeId?: string;
  priority?: TaskPriority;
  status?: TaskItemStatus;
  onlyMyTasks?: boolean;
}

export interface TaskAttachmentDto {
  fileName?: string;
  fileContent?: string;
}

export interface TaskDto extends FullAuditedEntityDto<string> {
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
