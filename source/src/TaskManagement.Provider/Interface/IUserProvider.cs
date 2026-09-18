using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using TaskManagement.Users;

namespace TaskManagement.Provider.Interface
{
    public interface IUserProvider
    {
        Task<PagedResultDto<UserDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting);
        Task<UserDto?> GetByIdAsync(Guid id);
        Task CreateAsync(CreateUpdateUserDto input);
        Task UpdateAsync(Guid id, CreateUpdateUserDto input);
        Task DeleteAsync(Guid id);
    }
}