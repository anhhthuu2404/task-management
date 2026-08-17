import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface AssignUserToDepartmentDto {
  userId?: string;
  departmentId?: string;
  isManager: boolean;
}

export interface CreateUpdateDepartmentDto {
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface DepartmentDto extends FullAuditedEntityDto<string> {
  code?: string;
  name?: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
}

export interface DepartmentTreeDto extends DepartmentDto {
  children: DepartmentTreeDto[];
}
