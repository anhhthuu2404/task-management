import type { CreationAuditedEntityDto } from '@abp/ng.core';

export interface TaskHistoryDto extends CreationAuditedEntityDto<string> {
  taskId?: string;
  action?: string;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  oldAssigneeId?: string;
  oldAssigneeName?: string;
  newAssigneeId?: string;
  newAssigneeName?: string;
}
