using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using TaskManagement.Projects;

namespace TaskManagement.Projects
{
    public interface IProjectAppService : ICrudAppService<
        ProjectDto,
        Guid,
        ProjectListFilterDto,
        CreateUpdateProjectDto,
        CreateUpdateProjectDto>
    {
        Task<ListResultDto<MilestoneDto>> GetMilestonesAsync(Guid projectId);
        Task<MilestoneDto> CreateMilestoneAsync(Guid projectId, CreateUpdateMilestoneDto input);
        Task DeleteMilestoneAsync(Guid milestoneId);

        Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId);
        Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto input);
        Task RemoveMemberAsync(Guid memberId);
    }
}