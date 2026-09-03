import type { MilestoneStatus } from './milestone-status.enum';
import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface AddProjectMemberDto {
  userId?: string;
  role?: string;
}

export interface CreateUpdateMilestoneDto {
  title?: string;
  description?: string;
  dueDate?: string;
  status?: MilestoneStatus;
}

export interface CreateUpdateProjectDto {
  name?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
}

export interface MilestoneDto extends EntityDto<string> {
  projectId?: string;
  title?: string;
  description?: string;
  dueDate?: string;
  status?: MilestoneStatus;
}

export interface ProjectDto extends FullAuditedEntityDto<string> {
  name?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
  memberCount?: number;
  milestoneCount?: number;
}

export interface ProjectListFilterDto extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: string;
}

export interface ProjectMemberDto extends EntityDto<string> {
  projectId?: string;
  userId?: string;
  userName?: string;
  role?: string;
}
