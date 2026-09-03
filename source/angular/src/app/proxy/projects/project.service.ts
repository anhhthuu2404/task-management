import type { AddProjectMemberDto, CreateUpdateMilestoneDto, CreateUpdateProjectDto, MilestoneDto, ProjectDto, ProjectListFilterDto, ProjectMemberDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  addMember = (projectId: string, input: AddProjectMemberDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectMemberDto>({
      method: 'POST',
      url: `/api/app/project/member/${projectId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateProjectDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'POST',
      url: '/api/app/project',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createMilestone = (projectId: string, input: CreateUpdateMilestoneDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MilestoneDto>({
      method: 'POST',
      url: `/api/app/project/milestone/${projectId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/project/${id}`,
    },
    { apiName: this.apiName,...config });
  

  deleteMilestone = (milestoneId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/project/milestone/${milestoneId}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'GET',
      url: `/api/app/project/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: ProjectListFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ProjectDto>>({
      method: 'GET',
      url: '/api/app/project',
      params: { filter: input.filter, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMembers = (projectId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<ProjectMemberDto>>({
      method: 'GET',
      url: `/api/app/project/members/${projectId}`,
    },
    { apiName: this.apiName,...config });
  

  getMilestones = (projectId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<MilestoneDto>>({
      method: 'GET',
      url: `/api/app/project/milestones/${projectId}`,
    },
    { apiName: this.apiName,...config });
  

  removeMember = (memberId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/project/member/${memberId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateProjectDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'PUT',
      url: `/api/app/project/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}