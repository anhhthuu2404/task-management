using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Departments;

public interface IDepartmentAppService :
    ICrudAppService<
        DepartmentDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateDepartmentDto>
{
    // Lấy danh sách phòng ban dạng cây
    Task<List<DepartmentTreeDto>> GetTreeAsync();

    // Gán User vào phòng ban qua DTO
    Task AssignUserAsync(AssignUserToDepartmentDto input);

    // Gán User vào phòng ban trực tiếp kèm quyền Manager (nếu dùng)
    Task AssignUserToDepartmentAsync(Guid userId, Guid departmentId, bool isManager);
}