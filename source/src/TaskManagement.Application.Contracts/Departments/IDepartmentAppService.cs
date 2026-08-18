using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Departments;

public interface IDepartmentAppService :
    ICrudAppService<
        DepartmentDto,
        Guid,
        GetDepartmentListDto, // Dùng DTO này để hỗ trợ tìm kiếm/lọc
        CreateUpdateDepartmentDto>
{
    Task<List<DepartmentTreeDto>> GetTreeAsync();
    Task AssignUserAsync(AssignUserToDepartmentDto input);
    Task AssignUserToDepartmentAsync(Guid userId, Guid departmentId, bool isManager);
}