import { mapEnumToOptions } from '@abp/ng.core';

export enum TaskItemStatus {
  New = 0,
  InProgress = 1,
  InReview = 2,
  Completed = 3,
  Canceled = 4,
}

export const taskItemStatusOptions = mapEnumToOptions(TaskItemStatus);
