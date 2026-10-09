using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Roles
{
    public interface IRoleAppService : IApplicationService
    {
        Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting);
        Task<RoleDto?> GetByIdAsync(Guid id);
        Task<RoleDto> CreateAsync(CreateUpdateRoleDto input);
        Task<RoleDto> UpdateAsync(Guid id, CreateUpdateRoleDto input);
        Task DeleteAsync(Guid id);
    }
}