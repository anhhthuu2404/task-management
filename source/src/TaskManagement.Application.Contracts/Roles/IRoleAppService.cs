using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using TaskManagement.Roles; 

namespace TaskManagement.Roles
{
    public interface IRoleAppService : IApplicationService
    {
        Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting);
        Task<RoleDto?> GetByIdAsync(Guid id);
        Task CreateAsync(RoleDto input);
        Task UpdateAsync(Guid id, CreateUpdateRoleDto input);
        Task DeleteAsync(Guid id);
    }
}