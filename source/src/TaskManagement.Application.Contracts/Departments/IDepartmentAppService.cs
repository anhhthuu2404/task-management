using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Departments;

public interface IDepartmentAppService : ICrudAppService<
    DepartmentDto,
    Guid,
    GetDepartmentListDto,
    CreateUpdateDepartmentDto>
{
    Task<List<DepartmentTreeDto>> GetTreeAsync();
    Task AssignUserAsync(AssignUserToDepartmentDto input);
    Task DeleteUserAsync(Guid departmentId, Guid userId);
}