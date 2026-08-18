using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Localization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Departments;

[Authorize(TaskManagementPermissions.Departments.Default)]
public class DepartmentAppService : CrudAppService<
    Department,
    DepartmentDto,
    Guid,
    GetDepartmentListDto,
    CreateUpdateDepartmentDto>, IDepartmentAppService
{
    private readonly IRepository<UserDepartment> _userDepartmentRepository;

    public DepartmentAppService(
        IRepository<Department, Guid> repository,
        IRepository<UserDepartment> userDepartmentRepository)
        : base(repository)
    {
        _userDepartmentRepository = userDepartmentRepository;

        // Khởi tạo Phân quyền & Tài nguyên đa ngôn ngữ chuẩn ABP trong Constructor
        LocalizationResource = typeof(TaskManagementResource);
        GetPolicyName = TaskManagementPermissions.Departments.Default;
        GetListPolicyName = TaskManagementPermissions.Departments.Default;
        CreatePolicyName = TaskManagementPermissions.Departments.Create;
        UpdatePolicyName = TaskManagementPermissions.Departments.Edit;
        DeletePolicyName = TaskManagementPermissions.Departments.Delete;
    }

    protected override async Task<IQueryable<Department>> CreateFilteredQueryAsync(GetDepartmentListDto input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            query = query.Where(x => x.Name.Contains(input.Filter) || x.Code.Contains(input.Filter));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        return query;
    }

    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        var departments = await Repository.GetListAsync();
        var departmentDtos = ObjectMapper.Map<List<Department>, List<DepartmentTreeDto>>(departments);

        var lookup = departmentDtos.ToLookup(x => x.ParentId);

        // Chuẩn hóa tên tham số parentId để tránh lỗi CS0103
        List<DepartmentTreeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId].Select(node =>
            {
                node.Children = BuildTree(node.Id);
                return node;
            }).ToList();
        }

        return BuildTree(null);
    }

    public async Task AssignUserAsync(AssignUserToDepartmentDto input)
    {
        await AssignUserToDepartmentAsync(input.UserId, input.DepartmentId, input.IsManager);
    }

    public async Task AssignUserToDepartmentAsync(Guid userId, Guid departmentId, bool isManager)
    {
        var existing = await _userDepartmentRepository.FindAsync(x => x.UserId == userId && x.DepartmentId == departmentId);

        if (existing == null)
        {
            await _userDepartmentRepository.InsertAsync(new UserDepartment
            {
                UserId = userId,
                DepartmentId = departmentId,
                IsManager = isManager
            });
        }
        else
        {
            existing.IsManager = isManager;
            await _userDepartmentRepository.UpdateAsync(existing);
        }
    }
}