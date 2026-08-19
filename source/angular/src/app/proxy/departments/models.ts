import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface AssignUserToDepartmentDto {
  userId?: string;
  departmentId?: string;
  isManager: boolean;
}

export interface CreateUpdateDepartmentDto {
  code: string;
  name: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
}

export interface DepartmentDto extends FullAuditedEntityDto<string> {
  code?: string;
  name?: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
  members: DepartmentMemberDto[];
}

export interface DepartmentMemberDto {
  userId?: string;
  userName?: string;
  email?: string;
  isManager: boolean;
}

export interface DepartmentTreeDto extends DepartmentDto {
  children: DepartmentTreeDto[];
}

export interface GetDepartmentListDto extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}
