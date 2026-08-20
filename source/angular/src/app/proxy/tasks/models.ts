import type { IRemoteStreamContent } from '../volo/abp/content/models';
import type { EntityDto } from '@abp/ng.core';

export interface CreateTaskInputDto {
  title: string;
  description?: string;
  priority?: number;
  dueDate?: string;
  categoryId: string;
  assigneeId?: string;
  files: IRemoteStreamContent[];
}

export interface TaskDto extends EntityDto<string> {
  title?: string;
  description?: string;
  priority?: number;
  dueDate?: string;
  categoryId?: string;
  assigneeId?: string;
}
