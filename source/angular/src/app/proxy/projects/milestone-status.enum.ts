import { mapEnumToOptions } from '@abp/ng.core';

export enum MilestoneStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
}

export const milestoneStatusOptions = mapEnumToOptions(MilestoneStatus);
