using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Projects
{
    public class ProjectAppService : CrudAppService<
        Project,
        ProjectDto,
        Guid,
        ProjectListFilterDto,
        CreateUpdateProjectDto,
        CreateUpdateProjectDto>,
        IProjectAppService
    {
        private readonly IRepository<ProjectMilestone, Guid> _milestoneRepository;
        private readonly IRepository<ProjectMember, Guid> _memberRepository;

        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<ProjectMilestone, Guid> milestoneRepository,
            IRepository<ProjectMember, Guid> memberRepository) : base(repository)
        {
            _milestoneRepository = milestoneRepository;
            _memberRepository = memberRepository;
        }

        protected override async Task<IQueryable<Project>> CreateFilteredQueryAsync(ProjectListFilterDto input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (input == null)
            {
                return query;
            }

            return query
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!) || (x.Description != null && x.Description.Contains(input.Filter!)))
                .WhereIf(!string.IsNullOrWhiteSpace(input.Status), x => x.Status == input.Status);
        }

        public async Task<ListResultDto<MilestoneDto>> GetMilestonesAsync(Guid projectId)
        {
            var milestones = await _milestoneRepository.GetListAsync(x => x.ProjectId == projectId);
            return new ListResultDto<MilestoneDto>(ObjectMapper.Map<List<ProjectMilestone>, List<MilestoneDto>>(milestones));
        }

        public async Task<MilestoneDto> CreateMilestoneAsync(Guid projectId, CreateUpdateMilestoneDto input)
        {
            var milestone = new ProjectMilestone
            {
                ProjectId = projectId,
                Title = input.Title,
                Description = input.Description,
                DueDate = input.DueDate,
                Status = input.Status,
                AssigneeUserId = input.AssigneeUserId
            };

            await _milestoneRepository.InsertAsync(milestone);
            return ObjectMapper.Map<ProjectMilestone, MilestoneDto>(milestone);
        }

        public async Task DeleteMilestoneAsync(Guid milestoneId)
        {
            await _milestoneRepository.DeleteAsync(milestoneId);
        }

        public async Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            var members = await _memberRepository.GetListAsync(x => x.ProjectId == projectId);
            return new ListResultDto<ProjectMemberDto>(ObjectMapper.Map<List<ProjectMember>, List<ProjectMemberDto>>(members));
        }

        public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto input)
        {
            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = input.UserId,
                Role = input.Role
            };

            await _memberRepository.InsertAsync(member);
            return ObjectMapper.Map<ProjectMember, ProjectMemberDto>(member);
        }

        public async Task RemoveMemberAsync(Guid memberId)
        {
            await _memberRepository.DeleteAsync(memberId);
        }
    }
}