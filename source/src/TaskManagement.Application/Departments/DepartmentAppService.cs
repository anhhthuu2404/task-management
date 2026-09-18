using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Localization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace TaskManagement.Departments;

[Authorize(TaskManagementPermissions.Departments.Default)]
public class DepartmentAppService : ApplicationService, IDepartmentAppService
{
    private readonly IDepartmentProvider _departmentProvider;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;

    public DepartmentAppService(
        IDepartmentProvider departmentProvider,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager)
    {
        _departmentProvider = departmentProvider;
        _userManager = userManager;
        _roleManager = roleManager;

        LocalizationResource = typeof(TaskManagementResource);
    }

    public async Task<DepartmentDto> GetAsync(Guid id)
    {
        var dto = await _departmentProvider.GetByIdAsync(id);
        dto.Members = await GetUsersAsync(id);
        return dto;
    }

    public async Task<PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input)
    {
        var pagedResult = await _departmentProvider.GetListAsync(input);

        // Gắn danh sách thành viên cho từng phòng ban trong danh sách
        foreach (var department in pagedResult.Items)
        {
            department.Members = await GetUsersAsync(department.Id);
        }

        return pagedResult;
    }

    [Authorize(TaskManagementPermissions.Departments.Create)]
    public async Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
    {
        var id = Guid.NewGuid();
        await _departmentProvider.CreateAsync(input, id, CurrentUser.Id);
        return await GetAsync(id);
    }

    [Authorize(TaskManagementPermissions.Departments.Edit)]
    public async Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
    {
        await _departmentProvider.UpdateAsync(id, input, CurrentUser.Id);
        return await GetAsync(id);
    }

    [Authorize(TaskManagementPermissions.Departments.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _departmentProvider.DeleteAsync(id, CurrentUser.Id);
    }

    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        return await _departmentProvider.GetTreeAsync();
    }

    public async Task<List<DepartmentMemberDto>> GetUsersAsync(Guid id)
    {
        // Chuyển logic truy vấn qua Provider để tuân thủ kiến trúc
        return await _departmentProvider.GetUsersByDepartmentIdAsync(id);
    }

    [UnitOfWork]
    public async Task AssignUserAsync(AssignUserToDepartmentDto input)
    {
        // 1. Cập nhật/Thêm phân quyền cho phòng ban hiện tại (thông qua Provider)
        await _departmentProvider.UpsertUserDepartmentAsync(input.UserId, input.DepartmentId, input.IsManager);
        await SyncUserRoleAsync(input.UserId, input.DepartmentId);

        // 2. Nếu gán làm Trưởng phòng, tự động đồng bộ xuống các phòng ban con
        if (input.IsManager)
        {
            await SyncManagerToChildDepartmentsRecursiveAsync(input.DepartmentId, input.UserId);
        }
    }

    public async Task DeleteUserAsync(Guid departmentId, Guid userId)
    {
        // Xóa liên kết thông qua Provider
        var deleted = await _departmentProvider.DeleteUserDepartmentAsync(departmentId, userId);
        if (deleted)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var departmentDto = await _departmentProvider.GetByIdAsync(departmentId);

            if (user != null && departmentDto != null && !string.IsNullOrWhiteSpace(departmentDto.Code))
            {
                string roleName = departmentDto.Code.Trim();
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleName);
                }
            }
        }
    }

    #region Helper Methods (Đồng bộ phân cấp phòng ban)

    private async Task SyncUserRoleAsync(Guid userId, Guid departmentId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var departmentDto = await _departmentProvider.GetByIdAsync(departmentId);

        if (user != null && departmentDto != null && !string.IsNullOrWhiteSpace(departmentDto.Code))
        {
            string roleName = departmentDto.Code.Trim();
            var roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (roleExists && !await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
    }

    private async Task SyncManagerToChildDepartmentsRecursiveAsync(Guid parentId, Guid userId)
    {
        var allTree = await _departmentProvider.GetTreeAsync();
        var parentNode = FindDepartmentInTree(allTree, parentId);

        if (parentNode?.Children != null)
        {
            foreach (var child in parentNode.Children)
            {
                await _departmentProvider.UpsertUserDepartmentAsync(userId, child.Id, true);
                await SyncUserRoleAsync(userId, child.Id);

                // Đệ quy xuống các cấp cháu sâu hơn
                await SyncManagerToChildDepartmentsRecursiveAsync(child.Id, userId);
            }
        }
    }

    private static DepartmentTreeDto? FindDepartmentInTree(List<DepartmentTreeDto> list, Guid id)
    {
        foreach (var node in list)
        {
            if (node.Id == id) return node;
            if (node.Children != null && node.Children.Count > 0)
            {
                var found = FindDepartmentInTree(node.Children, id);
                if (found != null) return found;
            }
        }
        return null;
    }

    #endregion
}