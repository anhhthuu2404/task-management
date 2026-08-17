using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Departments;

public class DepartmentAppService :
    CrudAppService<
        Department,
        DepartmentDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateDepartmentDto>,
    IDepartmentAppService
{
    private readonly IRepository<UserDepartment> _userDepartmentRepository;

    public DepartmentAppService(
        IRepository<Department, Guid> repository,
        IRepository<UserDepartment> userDepartmentRepository)
        : base(repository)
    {
        _userDepartmentRepository = userDepartmentRepository;

        GetPolicyName = TaskManagementPermissions.Departments.Default;
        GetListPolicyName = TaskManagementPermissions.Departments.Default;
        CreatePolicyName = TaskManagementPermissions.Departments.Create;
        UpdatePolicyName = TaskManagementPermissions.Departments.Edit;
        DeletePolicyName = TaskManagementPermissions.Departments.Delete;
    }

    // 1. Triển khai phương thức dựng cây sơ đồ phòng ban
    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        await CheckPolicyAsync(TaskManagementPermissions.Departments.Default);

        var departments = await Repository.GetListAsync();
        var dtos = ObjectMapper.Map<List<Department>, List<DepartmentTreeDto>>(departments);

        var lookup = dtos.ToLookup(x => x.ParentId);
        foreach (var item in dtos)
        {
            item.Children = lookup[item.Id].ToList();
        }

        return dtos.Where(x => x.ParentId == null).ToList();
    }

    // 2. Gán User vào phòng ban qua DTO
    public async Task AssignUserAsync(AssignUserToDepartmentDto input)
    {
        await AssignUserToDepartmentAsync(input.UserId, input.DepartmentId, input.IsManager);
    }

    // 3. Gán User vào phòng ban qua bảng trung gian UserDepartment
    public async Task AssignUserToDepartmentAsync(Guid userId, Guid departmentId, bool isManager)
    {
        await CheckPolicyAsync(TaskManagementPermissions.Departments.AssignUser);

        await _userDepartmentRepository.DeleteAsync(x => x.UserId == userId && x.DepartmentId == departmentId);
        await _userDepartmentRepository.InsertAsync(new UserDepartment
        {
            UserId = userId,
            DepartmentId = departmentId,
            IsManager = isManager
        });
    }
}