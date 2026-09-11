using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Localization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

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
    private readonly IRepository<Volo.Abp.Identity.IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;

    public DepartmentAppService(
        IRepository<Department, Guid> repository,
        IRepository<UserDepartment> userDepartmentRepository,
        IRepository<Volo.Abp.Identity.IdentityUser, Guid> userRepository,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager)
        : base(repository)
    {
        _userDepartmentRepository = userDepartmentRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;

        LocalizationResource = typeof(TaskManagementResource);
        GetPolicyName = TaskManagementPermissions.Departments.Default;
        GetListPolicyName = TaskManagementPermissions.Departments.Default;
        CreatePolicyName = TaskManagementPermissions.Departments.Create;
        UpdatePolicyName = TaskManagementPermissions.Departments.Edit;
        DeletePolicyName = TaskManagementPermissions.Departments.Delete;
    }

    public override async Task<DepartmentDto> GetAsync(Guid id)
    {
        var department = await Repository.GetAsync(id);

        var dto = new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            ParentId = department.ParentId,
            IsActive = department.IsActive,
            CreationTime = department.CreationTime,
            CreatorId = department.CreatorId,
            LastModificationTime = department.LastModificationTime,
            LastModifierId = department.LastModifierId,
            IsDeleted = department.IsDeleted,
            DeleterId = department.DeleterId,
            DeletionTime = department.DeletionTime,
            Members = []
        };

        var query = from userDept in await _userDepartmentRepository.GetQueryableAsync()
                    join user in await _userRepository.GetQueryableAsync() on userDept.UserId equals user.Id
                    where userDept.DepartmentId == id
                    select new DepartmentMemberDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        IsManager = userDept.IsManager
                    };

        dto.Members = await AsyncExecuter.ToListAsync(query);

        return dto;
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
    public async Task<List<DepartmentMemberDto>> GetUsersAsync(Guid id)
    {
        var query = from userDept in await _userDepartmentRepository.GetQueryableAsync()
                    join user in await _userRepository.GetQueryableAsync() on userDept.UserId equals user.Id
                    where userDept.DepartmentId == id
                    select new DepartmentMemberDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        IsManager = userDept.IsManager
                    };

        return await AsyncExecuter.ToListAsync(query);
    }

    public async Task AssignUserAsync(AssignUserToDepartmentDto input)
    {
        var existing = await _userDepartmentRepository.FirstOrDefaultAsync(x => x.UserId == input.UserId && x.DepartmentId == input.DepartmentId);

        if (existing == null)
        {
            await _userDepartmentRepository.InsertAsync(new UserDepartment
            {
                UserId = input.UserId,
                DepartmentId = input.DepartmentId,
                IsManager = input.IsManager
            });
        }
        else
        {
            existing.IsManager = input.IsManager;
            await _userDepartmentRepository.UpdateAsync(existing);
        }

        var user = await _userManager.FindByIdAsync(input.UserId.ToString());
        var department = await Repository.GetAsync(input.DepartmentId);

        if (user != null && department != null && !string.IsNullOrWhiteSpace(department.Code))
        {
            string roleName = department.Code.Trim();
            var roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (roleExists && !await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
    }

    public async Task DeleteUserAsync(Guid departmentId, Guid userId)
    {
        var existing = await _userDepartmentRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId);
        if (existing != null)
        {
            await _userDepartmentRepository.DeleteAsync(existing);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            var department = await Repository.GetAsync(departmentId);

            if (user != null && department != null && !string.IsNullOrWhiteSpace(department.Code))
            {
                string roleName = department.Code.Trim();
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleName);
                }
            }
        }
    }
    public override async Task<Volo.Abp.Application.Dtos.PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input)
    {
        var query = await CreateFilteredQueryAsync(input);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var departments = await AsyncExecuter.ToListAsync(query);

        var departmentDtos = new List<DepartmentDto>();

        // Duyệt qua từng phòng ban để map và lấy danh sách thành viên tương ứng
        foreach (var department in departments)
        {
            var dto = ObjectMapper.Map<Department, DepartmentDto>(department);
            dto.Members = await GetUsersAsync(department.Id);
            departmentDtos.Add(dto);
        }

        return new Volo.Abp.Application.Dtos.PagedResultDto<DepartmentDto>(
            totalCount,
            departmentDtos
        );
    }
}