using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using TaskManagement.Provider.Interface;
using TaskManagement.Roles; 

namespace TaskManagement.Roles
{
    public class RoleAppService : TaskManagementAppService, IRoleAppService
    {
        private readonly IRoleProvider _roleProvider;

        public RoleAppService(IRoleProvider roleProvider)
        {
            _roleProvider = roleProvider;
        }

        public async Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting)
        {
            return await _roleProvider.GetListAsync(filter, skipCount, maxResultCount, sorting);
        }

        public async Task<RoleDto?> GetByIdAsync(Guid id)
        {
            return await _roleProvider.GetByIdAsync(id);
        }

        public async Task CreateAsync(RoleDto input)
        {
            await _roleProvider.CreateAsync(input);
        }

        public async Task UpdateAsync(Guid id, CreateUpdateRoleDto input)
        {
            await _roleProvider.UpdateAsync(id, input);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _roleProvider.DeleteAsync(id);
        }
    }
}