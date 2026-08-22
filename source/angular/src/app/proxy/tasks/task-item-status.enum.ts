import { mapEnumToOptions } from '@abp/ng.core';

export enum TaskItemStatus {
  New = 0,
  InProcess = 1,
  InProgress = 1,
  Completed = 2,
  Canceled = 3,
}

export const taskItemStatusOptions = mapEnumToOptions(TaskItemStatus);
