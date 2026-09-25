using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Provider.Request;
using TaskManagement.Provider.Response;

namespace TaskManagement.Provider.Interface
{
    public interface IProjectProvider
    {
        // Project cơ bản
        Task<List<ProjectQueryResponse>> GetListAsync(ProjectGetListRequest request);
        Task<ProjectQueryResponse?> GetByIdAsync(Guid id);
        Task CreateAsync(ProjectQueryResponse input);
        Task UpdateAsync(ProjectQueryResponse input);
        Task DeleteAsync(Guid id);

        // --- Milestone Management ---
        Task<List<ProjectMilestoneResponse>> GetMilestonesByProjectIdAsync(Guid projectId);
        Task<ProjectMilestoneResponse?> GetMilestoneByIdAsync(Guid id);
        Task CreateMilestoneAsync(ProjectMilestoneResponse input);
        Task UpdateMilestoneAsync(ProjectMilestoneResponse input);
        Task DeleteMilestoneAsync(Guid id);

        // --- Member Management ---
        Task<List<ProjectMemberResponse>> GetMembersByProjectIdAsync(Guid projectId);
        Task AddMemberAsync(ProjectMemberResponse input);
        Task RemoveMemberAsync(Guid id);
    }
}