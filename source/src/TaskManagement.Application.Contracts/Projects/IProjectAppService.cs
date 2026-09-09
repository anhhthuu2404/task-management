using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Projects
{
    public interface IProjectAppService : ICrudAppService<
        ProjectDto,
        Guid,
        ProjectListFilterDto,
        CreateUpdateProjectDto,
        CreateUpdateProjectDto>
    {
     
        Task<List<MilestoneDto>> GetMilestonesByProjectAsync(Guid projectId);
        Task<MilestoneDto> CreateMilestoneAsync(Guid projectId, CreateUpdateMilestoneDto input);
        Task DeleteMilestoneAsync(Guid milestoneId);

        [HttpGet("/api/app/project/by-project/{projectId}/members")]
        Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId);

        Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto input);
        Task RemoveMemberAsync(Guid memberId);
    }
}