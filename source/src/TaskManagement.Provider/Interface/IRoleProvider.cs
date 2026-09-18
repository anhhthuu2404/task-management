using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using TaskManagement.Roles; // Đã đổi sang namespace chứa RoleDto chuẩn

namespace TaskManagement.Provider.Interface
{
    public interface IRoleProvider
    {
        Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting);
        Task<RoleDto?> GetByIdAsync(Guid id);
        Task CreateAsync(RoleDto input);
        Task UpdateAsync(Guid id, CreateUpdateRoleDto input);
        Task DeleteAsync(Guid id);
    }
}