using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Departments;

public interface IDepartmentProvider
{
    Task<PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input, string culture = "vi");
    Task<DepartmentDto> GetByIdAsync(Guid id, string culture = "vi");
    Task CreateAsync(CreateUpdateDepartmentDto input, Guid id, Guid? creatorId);
    Task UpdateAsync(Guid id, CreateUpdateDepartmentDto input, Guid? modifierId);
    Task DeleteAsync(Guid id, Guid? deleterId);
    Task<List<DepartmentTreeDto>> GetTreeAsync(string culture = "vi");

    // Xử lý thành viên phòng ban (UserDepartment)
    Task<List<DepartmentMemberDto>> GetUsersByDepartmentIdAsync(Guid departmentId);
    Task UpsertUserDepartmentAsync(Guid userId, Guid departmentId, bool isManager);
    Task<bool> DeleteUserDepartmentAsync(Guid departmentId, Guid userId);
}